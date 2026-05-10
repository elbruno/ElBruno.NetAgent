using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ElBruno.NetAgent.Services;

namespace ElBruno.NetAgent.Tests
{
    public class TrayIconServiceExitTests
    {
        private IServiceProvider BuildServiceProvider(IHostApplicationLifetime lifetime)
        {
            var services = new ServiceCollection();
            // Mirror minimal registrations from Program.cs required by TrayIconService
            services.AddSingleton<Core.Configuration.IConfigurationService, ElBruno.NetAgent.Services.ConfigurationService>();
            services.AddSingleton(provider => Microsoft.Extensions.Options.Options.Create(provider.GetRequiredService<Core.Configuration.IConfigurationService>().GetOptionsAsync(System.Threading.CancellationToken.None).GetAwaiter().GetResult()));
            services.AddSingleton(provider => provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Core.Configuration.NetAgentOptions>>().Value);

            services.AddSingleton<ElBruno.NetAgent.Core.Services.INetworkInventoryService, ElBruno.NetAgent.Services.Network.NetworkInventoryService>();
            services.AddSingleton<ElBruno.NetAgent.Core.Services.INetworkQualityTester, ElBruno.NetAgent.Services.Network.NullNetworkQualityTester>();
            services.AddSingleton<ElBruno.NetAgent.Core.Services.INetworkQualityMonitor, ElBruno.NetAgent.Services.Network.NetworkQualityMonitor>();
            services.AddSingleton<ElBruno.NetAgent.Core.Decision.IDecisionEngine, TestDecisionEngine>();

            services.AddTransient<ElBruno.NetAgent.Interfaces.IStatusViewModel, ElBruno.NetAgent.ViewModels.StatusViewModel>();
            services.AddTransient<ElBruno.NetAgent.Views.StatusWindow>();

            services.AddTransient<ElBruno.NetAgent.Interfaces.ISettingsViewModel, ElBruno.NetAgent.ViewModels.SettingsViewModel>();
            services.AddTransient<ElBruno.NetAgent.Views.SettingsWindow>();

            services.AddTransient<ElBruno.NetAgent.Interfaces.INetworkSelectorViewModel, ElBruno.NetAgent.ViewModels.NetworkSelectorViewModel>();
            services.AddTransient<ElBruno.NetAgent.Views.NetworkSelectorWindow>();

            services.AddSingleton<Core.Services.IDialogService, NullDialogService>();

            services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(Microsoft.Extensions.Logging.Abstractions.NullLogger<>));

            // Register the test lifetime
            services.AddSingleton<IHostApplicationLifetime>(lifetime);

            // Register TrayIconService using factory so it receives the built provider
            services.AddSingleton<ElBruno.NetAgent.Services.TrayIconService>(provider =>
            {
                return new ElBruno.NetAgent.Services.TrayIconService(
                    provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ElBruno.NetAgent.Services.TrayIconService>>(),
                    provider.GetRequiredService<Core.Configuration.IConfigurationService>(),
                    provider.GetRequiredService<ElBruno.NetAgent.Core.Services.INetworkInventoryService>(),
                    provider.GetRequiredService<ElBruno.NetAgent.Core.Services.INetworkQualityMonitor>(),
                    provider.GetRequiredService<ElBruno.NetAgent.Core.Decision.IDecisionEngine>(),
                    lifetime,
                    provider);
            });

            return services.BuildServiceProvider();
        }

        private class TestHostApplicationLifetime : IHostApplicationLifetime
        {
            public CancellationToken ApplicationStarted { get; } = CancellationToken.None;
            public CancellationToken ApplicationStopping { get; } = CancellationToken.None;
            public CancellationToken ApplicationStopped { get; } = CancellationToken.None;
            public bool StopCalled { get; private set; }
            public void StopApplication() => StopCalled = true;
        }

        private class TestNotifyIconAdapter : ElBruno.NetAgent.Services.INotifyIconAdapter
        {
            public bool Visible { get; set; }
            public string Text { get; set; }
            public System.Drawing.Icon Icon { get; set; }
            public ContextMenuStrip ContextMenuStrip { get; set; }
            public bool IsDisposed { get; private set; }
            public void ShowBalloonTip(int timeout, string title, string text, ToolTipIcon icon) { }
            public void Dispose() { IsDisposed = true; }
        }

        [Fact]
        public void ExitClick_ShutdownsHost_And_DisposesTrayResources()
        {
            var lifetime = new TestHostApplicationLifetime();
            var sp = BuildServiceProvider(lifetime);

            // Execute test logic inline (no STA thread) since InvokeExitForTests_NoDispatch does not rely on WPF dispatcher.
            try
            {
                var logger = new Microsoft.Extensions.Logging.LoggerFactory().CreateLogger<ElBruno.NetAgent.Services.TrayIconService>();
                var testAdapter = new TestNotifyIconAdapter();
                var svc = new ElBruno.NetAgent.Services.TrayIconService(logger, lifetime, testAdapter);

                svc.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

                svc.InvokeExitForTests_NoDispatch();

                // Verify host lifetime was requested to stop
                Assert.True(lifetime.StopCalled, "IHostApplicationLifetime.StopApplication should be called");

                // Sanity: ensure the adapter reported disposed (if used)
                Assert.True(testAdapter.IsDisposed, "NotifyIcon adapter should be disposed after Exit");

                // Allow a short moment for dispose to occur
                System.Threading.SpinWait.SpinUntil(() =>
                {
                    return !svc.IsNotifyIconPresentForTests();
                }, TimeSpan.FromSeconds(5));

                // Adapter should be disposed or the service should report no tray icon present.
                Assert.True(testAdapter.IsDisposed || !svc.IsNotifyIconPresentForTests(), "Notify icon should be disposed or not present");
            }
            catch (Exception ex)
            {
                throw new AggregateException("Exception during Exit click test", ex);
            }
        }
    }
}
