using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using ElBruno.NetAgent.Interfaces;
using ElBruno.NetAgent.Views;
using ElBruno.NetAgent.Services;
using ElBruno.NetAgent.Core.Services;

namespace ElBruno.NetAgent.Tests
{
    public class TrayIntegrationTests
    {
        private IHost BuildHost()
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Mirror Program.cs registrations needed for tests
                    services.AddSingleton<Core.Configuration.IConfigurationService, ElBruno.NetAgent.Services.ConfigurationService>();
                    services.AddSingleton(provider => Microsoft.Extensions.Options.Options.Create(provider.GetRequiredService<Core.Configuration.IConfigurationService>().GetOptionsAsync(System.Threading.CancellationToken.None).GetAwaiter().GetResult()));
                    services.AddSingleton(provider => provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Core.Configuration.NetAgentOptions>>().Value);

                    services.AddSingleton<ElBruno.NetAgent.Core.Services.INetworkInventoryService, ElBruno.NetAgent.Services.Network.NetworkInventoryService>();
                    services.AddSingleton<ElBruno.NetAgent.Core.Services.INetworkQualityTester, ElBruno.NetAgent.Services.Network.NullNetworkQualityTester>();
                    services.AddSingleton<ElBruno.NetAgent.Core.Services.INetworkQualityMonitor, ElBruno.NetAgent.Services.Network.NetworkQualityMonitor>();
                    services.AddSingleton<ElBruno.NetAgent.Core.Decision.IDecisionEngine, TestDecisionEngine>();

                    services.AddTransient<IStatusViewModel, ElBruno.NetAgent.ViewModels.StatusViewModel>();
                    services.AddTransient<StatusWindow>();

                    services.AddTransient<ISettingsViewModel, ElBruno.NetAgent.ViewModels.SettingsViewModel>();
                    services.AddTransient<ElBruno.NetAgent.Views.SettingsWindow>();

                    services.AddTransient<INetworkSelectorViewModel, ElBruno.NetAgent.ViewModels.NetworkSelectorViewModel>();
                    services.AddTransient<ElBruno.NetAgent.Views.NetworkSelectorWindow>();

                    // Register NullDialogService as safety for tests
                    services.AddSingleton<Core.Services.IDialogService, NullDialogService>();

                    services.AddSingleton<TrayIconService>();
                })
                .Build();

            return host;
        }

        [Fact(Skip = "Integration: requires OS resources - skipped in unit runs")]
        public void DI_Resolves_CoreServices()
        {
            using var host = BuildHost();
            var sp = host.Services;

            Assert.NotNull(sp.GetService<IStatusViewModel>());
            Assert.NotNull(sp.GetService<ISettingsViewModel>());
            Assert.NotNull(sp.GetService<INetworkSelectorViewModel>());
            Assert.NotNull(sp.GetService<Core.Services.IDialogService>());
            Assert.NotNull(sp.GetService<TrayIconService>());
        }

        [Fact(Skip = "Integration: requires OS resources - skipped in unit runs")]
        public void OpenStatus_Window_CanBeConstructed_On_STA()
        {
            using var host = BuildHost();
            var sp = host.Services;

            var done = new ManualResetEvent(false);
            Exception? thrown = null;

            var thread = new Thread(() =>
            {
                try
                {
                    var vm = sp.GetService<IStatusViewModel>();
                    var window = new StatusWindow(vm);
                    window.Show();
                    window.Close();
                }
                catch (Exception ex)
                {
                    thrown = ex;
                }
                finally
                {
                    done.Set();
                    // Shutdown dispatcher if present
                    try { System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown(); } catch { }
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.True(done.WaitOne(TimeSpan.FromSeconds(30)), "STA thread did not complete in time");
            if (thrown != null) throw new AggregateException("Exception constructing StatusWindow on STA", thrown);
        }

        [Fact(Skip = "Integration: requires OS resources - skipped in unit runs")]
        public async Task PreviewBestSwitch_DryRun_DoesNotMutate()
        {
            using var host = BuildHost();
            var sp = host.Services;

            var inventory = sp.GetService<ElBruno.NetAgent.Core.Services.INetworkInventoryService>();
            var quality = sp.GetService<ElBruno.NetAgent.Core.Services.INetworkQualityMonitor>();
            var decision = sp.GetService<ElBruno.NetAgent.Core.Decision.IDecisionEngine>();

            Assert.NotNull(inventory);
            Assert.NotNull(quality);
            Assert.NotNull(decision);

            var list = await inventory!.GetInterfacesAsync(CancellationToken.None);
            // safe: reading interfaces only
            if (list != null && list.Count > 0)
            {
                var report = await quality!.EvaluateAsync(list.First(), CancellationToken.None);
                var opts = host.Services.GetService<Microsoft.Extensions.Options.IOptions<Core.Configuration.NetAgentOptions>>()!.Value;
                var result = await decision!.EvaluateAsync(report!, opts, CancellationToken.None);
                Assert.NotNull(result);
            }
        }

        [Fact(Skip = "Integration: requires OS resources - skipped in unit runs")]
        public void ExitRequestsHostShutdown_DoesNotThrow()
        {
            using var host = BuildHost();
            var lifetime = host.Services.GetService<IHostApplicationLifetime>();
            // Should not throw even if called when host not started
            lifetime?.StopApplication();
        }
    }
}
