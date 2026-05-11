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
            // detect smoke-test flags early and avoid starting hosted services or WPF loop when present
            var smokeTest = args != null && args.Any(a => string.Equals(a, "--smoke-test", StringComparison.OrdinalIgnoreCase));
            var smokeTestExit = args != null && args.Any(a => string.Equals(a, "--smoke-test-exit", StringComparison.OrdinalIgnoreCase));

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
                    services.TryAddSingleton<Core.Services.ILinkService, Services.NullLinkService>();
                    // Wire host stopping to WPF Application shutdown on the UI thread.
                    services.AddHostedService<Services.HostApplicationShutdownWiring>();
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

                    // Attempt to construct (but not show) key WPF windows to validate XAML runtime
                    try
                    {
                        try
                        {
                            var settingsWindow = host.Services.GetService<ElBruno.NetAgent.Views.SettingsWindow>();
                        }
                        catch (Exception ex)
                        {
                            logger?.LogError(ex, "SettingsWindow construction failed.");
                            failed = true;
                        }

                        try
                        {
                            var statusWindow = host.Services.GetService<ElBruno.NetAgent.Views.StatusWindow>();
                        }
                        catch (Exception ex)
                        {
                            logger?.LogError(ex, "StatusWindow construction failed.");
                            failed = true;
                        }

                        try
                        {
                            var nsWindow = host.Services.GetService<ElBruno.NetAgent.Views.NetworkSelectorWindow>();
                        }
                        catch (Exception ex)
                        {
                            logger?.LogError(ex, "NetworkSelectorWindow construction failed.");
                            failed = true;
                        }
                    }
                    catch { }

                    if (failed)
                    {
                        logger?.LogError("Smoke-test failed: one or more services or UI windows could not be resolved/constructed.");
                        host.Dispose();
                        return 1;
                    }

                    logger?.LogInformation("Smoke-test succeeded: all services resolved and UI windows constructed.");
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

            // Smoke test for SettingsViewModel Save behavior without touching disk
            var smokeTestSettingsSave = args != null && args.Any(a => string.Equals(a, "--smoke-test-settings-save", StringComparison.OrdinalIgnoreCase));
            if (smokeTestSettingsSave)
            {
                var logger = host.Services.GetService<ILoggerFactory>()?.CreateLogger("Program");
                logger?.LogInformation("Running smoke-test-settings-save: constructing SettingsViewModel with in-memory config service.");
                try
                {
                    var inmem = new ElBruno.NetAgent.Services.InMemoryConfigurationService();
                    var vm = new ElBruno.NetAgent.ViewModels.SettingsViewModel(inmem);

                    // Change a value to ensure Save composes something to persist
                    vm.PreferredInterfacePatternsText = "smoke-test";

                    // Execute save command (should be non-blocking)
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    vm.SaveCommand.Execute(null);
                    sw.Stop();
                    logger?.LogInformation("SaveCommand.Execute returned in {Elapsed}ms", sw.ElapsedMilliseconds);

                    // Wait up to 5 seconds for save to complete
                    var saved = inmem.AwaitSavedAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
                    if (!saved)
                    {
                        logger?.LogError("Settings save did not complete within timeout.");
                        return 1;
                    }

                    logger?.LogInformation("Settings save completed.");
                    return 0;
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Exception during smoke-test-settings-save.");
                    return 1;
                }
            }

            if (smokeTestExit)
            {
                var logger = host.Services.GetService<ILoggerFactory>()?.CreateLogger("Program");
                logger?.LogInformation("Running smoke-test-exit: starting host and invoking Exit flow.");

                try
                {
                    using (var startCts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10)))
                    {
                        host.StartAsync(startCts.Token).GetAwaiter().GetResult();
                    }

                    // Try to locate the TrayIconService instance from the host and invoke the Exit path used by tests.
                    ElBruno.NetAgent.Services.TrayIconService? tray = null;
                    try
                    {
                        tray = host.Services.GetService(typeof(ElBruno.NetAgent.Services.TrayIconService)) as ElBruno.NetAgent.Services.TrayIconService;
                        if (tray == null)
                        {
                            var hosted = host.Services.GetServices<Microsoft.Extensions.Hosting.IHostedService>();
                            tray = hosted?.OfType<ElBruno.NetAgent.Services.TrayIconService>().FirstOrDefault();
                        }
                    }
                    catch { }

                    if (tray != null)
                    {
                        try
                        {
                            // Invoke the non-dispatcher exit to avoid UI deadlocks in CI.
                            tray.InvokeExitForTests_NoDispatch();
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Failed invoking TrayIconService exit helper");
                        }
                    }
                    else
                    {
                        logger?.LogWarning("TrayIconService not found - cannot exercise Exit path");
                    }

                    // Request host stop and dispose cleanly with a timeout.
                    try
                    {
                        using (var stopCts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5)))
                        {
                            host.StopAsync(stopCts.Token).GetAwaiter().GetResult();
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, "Error stopping host during smoke-test-exit");
                    }

                    host.Dispose();
                    logger?.LogInformation("Smoke-test-exit completed.");
                    return 0;
                }
                catch (Exception ex)
                {
                    var loggerEx = host.Services.GetService<ILoggerFactory>()?.CreateLogger("Program");
                    loggerEx?.LogError(ex, "Exception during smoke-test-exit flow.");
                    try { host.Dispose(); } catch { }
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
