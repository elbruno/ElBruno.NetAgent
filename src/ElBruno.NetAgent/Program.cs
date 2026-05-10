using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ElBruno.NetAgent
{
    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            // detect smoke-test flag early and avoid starting hosted services or WPF loop when present
            var smokeTest = args != null && args.Any(a => string.Equals(a, "--smoke-test", StringComparison.OrdinalIgnoreCase));

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging => logging.AddConsole())
                .ConfigureServices((context, services) =>
                {
                    // Configuration service
                    services.AddSingleton<Core.Configuration.IConfigurationService, Services.ConfigurationService>();

                    // Provide IOptions<NetAgentOptions> synchronously at startup by reading the config.
                    services.AddSingleton(provider =>
                        Microsoft.Extensions.Options.Options.Create(
                            provider.GetRequiredService<Core.Configuration.IConfigurationService>()
                                .GetOptionsAsync(System.Threading.CancellationToken.None).GetAwaiter().GetResult()
                        )
                    );

                    // Also register NetAgentOptions concrete instance for services that depend on the POCO directly.
                    services.AddSingleton(provider =>
                        provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Core.Configuration.NetAgentOptions>>().Value
                    );

                    // Network inventory
                    services.AddSingleton<ElBruno.NetAgent.Core.Services.INetworkInventoryService, ElBruno.NetAgent.Services.Network.NetworkInventoryService>();

                    // Network quality tester (safe no-op default for UI/dry-run)
                    services.AddSingleton<ElBruno.NetAgent.Core.Services.INetworkQualityTester, ElBruno.NetAgent.Services.Network.NullNetworkQualityTester>();

                    // Network quality monitor (read-only)
                    services.AddSingleton<ElBruno.NetAgent.Core.Services.INetworkQualityMonitor, ElBruno.NetAgent.Services.Network.NetworkQualityMonitor>();

                    // Decision engine (in-memory, pure)
                    services.AddSingleton<ElBruno.NetAgent.Core.Decision.IDecisionEngine, ElBruno.NetAgent.Services.Decision.InMemoryDecisionEngine>();

                    services.AddTransient<ElBruno.NetAgent.Interfaces.IStatusViewModel, ElBruno.NetAgent.ViewModels.StatusViewModel>();
                    services.AddTransient<ElBruno.NetAgent.Views.StatusWindow>();

                    // Settings and Network Selector windows/viewmodels
                    services.AddTransient<ElBruno.NetAgent.Interfaces.ISettingsViewModel, ElBruno.NetAgent.ViewModels.SettingsViewModel>();
                    services.AddTransient<ElBruno.NetAgent.Views.SettingsWindow>();

                    services.AddTransient<ElBruno.NetAgent.Interfaces.INetworkSelectorViewModel, ElBruno.NetAgent.ViewModels.NetworkSelectorViewModel>();
                    services.AddTransient<ElBruno.NetAgent.Views.NetworkSelectorWindow>();

                    services.TryAddSingleton<Core.Services.IDialogService, Services.NullDialogService>();
                    services.AddHostedService<Services.TrayIconService>();

                    // Auto mode background service - evaluates decision engine on an interval and (dry-run first) requests switches.
                    services.AddHostedService<Services.AutoModeHostedService>();
                })
                .Build();

            if (smokeTest)
            {
                var logger = host.Services.GetService<ILoggerFactory>()?.CreateLogger("Program");
                logger?.LogInformation("Running smoke-test: resolving core services without starting hosted services or WPF loop.");
                try
                {
                    var cfg = host.Services.GetService<Core.Configuration.IConfigurationService>();
                    var inv = host.Services.GetService<ElBruno.NetAgent.Core.Services.INetworkInventoryService>();
                    var qm = host.Services.GetService<ElBruno.NetAgent.Core.Services.INetworkQualityMonitor>();
                    var de = host.Services.GetService<ElBruno.NetAgent.Core.Decision.IDecisionEngine>();

                    var failed = false;
                    if (cfg == null)
                    {
                        logger?.LogError("IConfigurationService resolution failed.");
                        failed = true;
                    }
                    if (inv == null)
                    {
                        logger?.LogError("INetworkInventoryService resolution failed.");
                        failed = true;
                    }
                    if (qm == null)
                    {
                        logger?.LogError("INetworkQualityMonitor resolution failed.");
                        failed = true;
                    }
                    if (de == null)
                    {
                        logger?.LogError("IDecisionEngine resolution failed.");
                        failed = true;
                    }

                    if (failed)
                    {
                        logger?.LogError("Smoke-test failed: one or more services could not be resolved.");
                        host.Dispose();
                        return 1;
                    }

                    logger?.LogInformation("Smoke-test succeeded: all services resolved.");
                    host.Dispose();
                    return 0;
                }
                catch (Exception ex)
                {
                    var loggerEx = host.Services.GetService<ILoggerFactory>()?.CreateLogger("Program");
                    loggerEx?.LogError(ex, "Exception during smoke-test service resolution.");
                    host.Dispose();
                    return 1;
                }
            }

            var app = new System.Windows.Application();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            app.Exit += (s, e) =>
            {
                var logger = host.Services.GetService<ILoggerFactory>()?.CreateLogger("Program");
                logger?.LogInformation("Application exiting - stopping host.");
                try
                {
                    using (var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5)))
                    {
                        host.StopAsync(cts.Token).GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error stopping host during Exit.");
                }
            };

            var loggerStart = host.Services.GetService<ILoggerFactory>()?.CreateLogger("Program");
            loggerStart?.LogInformation("Starting host.");
            host.StartAsync().GetAwaiter().GetResult();

            loggerStart?.LogInformation("Application started - running in tray.");
            try
            {
                app.Run();
            }
            finally
            {
                loggerStart?.LogInformation("Application Run returned - stopping host.");
                host.StopAsync().GetAwaiter().GetResult();
                host.Dispose();
            }

            return 0;
        }
    }
}
