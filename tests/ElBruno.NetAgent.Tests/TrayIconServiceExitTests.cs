using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ElBruno.NetAgent.Tests
{
    public class TrayIconServiceExitTests
    {
        private IHost BuildHost(IHostApplicationLifetime lifetime)
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
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

                    // Use singleton TrayIconService so tests can access instance
                    services.AddSingleton<ElBruno.NetAgent.Services.TrayIconService>();

                    // Override IHostApplicationLifetime with test double
                    services.AddSingleton<IHostApplicationLifetime>(lifetime);
                })
                .Build();

            return host;
        }

        private class TestHostApplicationLifetime : IHostApplicationLifetime
        {
            public CancellationToken ApplicationStarted { get; } = CancellationToken.None;
            public CancellationToken ApplicationStopping { get; } = CancellationToken.None;
            public CancellationToken ApplicationStopped { get; } = CancellationToken.None;
            public bool StopCalled { get; private set; }
            public void StopApplication() => StopCalled = true;
        }

        [Fact]
        public void ExitClick_ShutdownsHost_And_DisposesTrayResources()
        {
            var lifetime = new TestHostApplicationLifetime();
            using var host = BuildHost(lifetime);

            Exception? thrown = null;
            var done = new ManualResetEvent(false);

            var thread = new Thread(() =>
            {
                try
                {                    // Ensure a WPF application/dispatcher is present so StartAsync creates the tray.
                    var app = new System.Windows.Application();
                    app.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;

                    var svc = host.Services.GetService<ElBruno.NetAgent.Services.TrayIconService>();
                    Assert.NotNull(svc);

                    svc!.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

                    // Reflect to obtain the private menu and exit item.
                    var menuField = typeof(ElBruno.NetAgent.Services.TrayIconService).GetField("_menu", BindingFlags.NonPublic | BindingFlags.Instance);
                    var menu = menuField?.GetValue(svc) as ContextMenuStrip;
                    Assert.NotNull(menu);
                    ToolStripItem? exitItem = null;                    foreach (ToolStripItem it in menu.Items)
                    {                        if (string.Equals(it.Text, "Exit", StringComparison.OrdinalIgnoreCase)) { exitItem = it; break; }
                    }
                    Assert.NotNull(exitItem);
                    // Simulate clicking Exit
                    exitItem!.PerformClick();
                    // Verify host lifetime was requested to stop
                    Assert.True(lifetime.StopCalled, "IHostApplicationLifetime.StopApplication should be called");
                    // Verify _notifyIcon was disposed/cleared
                    var iconField = typeof(ElBruno.NetAgent.Services.TrayIconService).GetField("_notifyIcon", BindingFlags.NonPublic | BindingFlags.Instance);
                    var iconVal = iconField?.GetValue(svc);
                    Assert.Null(iconVal);
                }
                catch (Exception ex)
                {                    thrown = ex;
                }
                finally
                {                    done.Set();
                    try { System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown(); } catch { }
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.True(done.WaitOne(TimeSpan.FromSeconds(10)), "STA thread did not complete in time");
            if (thrown != null) throw new AggregateException("Exception during Exit click test", thrown);
        }
    }
}
