using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows.Forms;

namespace ElBruno.NetAgent.Services
{
    public class TrayIconService : IHostedService, IDisposable
    {
        private readonly ILogger<TrayIconService> _logger;
        private readonly IHostApplicationLifetime? _appLifetime;
        private NotifyIcon? _notifyIcon;
        private INotifyIconAdapter? _notifyIconAdapter;
        private ContextMenuStrip? _menu;
        private bool _autoMode;
        private readonly IServiceProvider? _serviceProvider;
        private readonly Core.Services.IDialogService _dialogService;
        private readonly Core.Services.ILinkService _linkService;

        public TrayIconService(ILogger<TrayIconService> logger,
            Core.Configuration.IConfigurationService configurationService,
            Core.Services.INetworkInventoryService inventoryService,
            Core.Services.INetworkQualityMonitor qualityMonitor,
            Core.Decision.IDecisionEngine decisionEngine,
            Core.Services.IDialogService dialogService,
            Core.Services.ILinkService linkService,
            IHostApplicationLifetime? appLifetime = null,
            IServiceProvider? serviceProvider = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appLifetime = appLifetime;
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
            _qualityMonitor = qualityMonitor ?? throw new ArgumentNullException(nameof(qualityMonitor));
            _decisionEngine = decisionEngine ?? throw new ArgumentNullException(nameof(decisionEngine));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _linkService = linkService ?? throw new ArgumentNullException(nameof(linkService));
            _serviceProvider = serviceProvider;
        }

        // Back-compat overload: older tests or registrations passed lifetime and serviceProvider as the 6th/7th args.
        // Preserve the previous parameter ordering by exposing an overload that accepts the appLifetime and serviceProvider
        // and supplies safe no-op dialog/link services.
        public TrayIconService(ILogger<TrayIconService> logger,
            Core.Configuration.IConfigurationService configurationService,
            Core.Services.INetworkInventoryService inventoryService,
            Core.Services.INetworkQualityMonitor qualityMonitor,
            Core.Decision.IDecisionEngine decisionEngine,
            IHostApplicationLifetime? appLifetime = null,
            IServiceProvider? serviceProvider = null)
            : this(logger, configurationService, inventoryService, qualityMonitor, decisionEngine, new ElBruno.NetAgent.Services.NullDialogService(), new ElBruno.NetAgent.Services.NullLinkService(), appLifetime, serviceProvider)
        {
        }

        private readonly Core.Configuration.IConfigurationService _configurationService;
        private readonly Core.Services.INetworkInventoryService _inventoryService;
        private readonly Core.Services.INetworkQualityMonitor _qualityMonitor;
        private readonly Core.Decision.IDecisionEngine _decisionEngine;

        // Back-compat constructor used by unit tests that instantiate the service directly.
        public TrayIconService(ILogger<TrayIconService> logger) : this(logger, new NullConfigurationService(), new ElBruno.NetAgent.Services.NullInventoryService(), new ElBruno.NetAgent.Services.NullQualityMonitor(), new ElBruno.NetAgent.Services.NullDecisionEngine(), new ElBruno.NetAgent.Services.NullDialogService(), new ElBruno.NetAgent.Services.NullLinkService(), null, null) { }

        // Internal ctor for tests to inject a test IHostApplicationLifetime and a test NotifyIcon instance (no changes to public API).
        internal TrayIconService(ILogger<TrayIconService> logger, IHostApplicationLifetime appLifetime, NotifyIcon notifyIcon)
            : this(logger, new NullConfigurationService(), new ElBruno.NetAgent.Services.NullInventoryService(), new ElBruno.NetAgent.Services.NullQualityMonitor(), new ElBruno.NetAgent.Services.NullDecisionEngine(), new ElBruno.NetAgent.Services.NullDialogService(), new ElBruno.NetAgent.Services.NullLinkService(), appLifetime, null)
        {
            _notifyIcon = notifyIcon;
        }

        // Internal ctor for tests to inject a test IHostApplicationLifetime and a test INotifyIconAdapter for deterministic disposal in unit tests.
        internal TrayIconService(ILogger<TrayIconService> logger, IHostApplicationLifetime appLifetime, INotifyIconAdapter notifyIconAdapter)
            : this(logger, new NullConfigurationService(), new ElBruno.NetAgent.Services.NullInventoryService(), new ElBruno.NetAgent.Services.NullQualityMonitor(), new ElBruno.NetAgent.Services.NullDecisionEngine(), new ElBruno.NetAgent.Services.NullDialogService(), new ElBruno.NetAgent.Services.NullLinkService(), appLifetime, null)
        {
            _notifyIconAdapter = notifyIconAdapter;
        }

        // A lightweight null implementation used when DI is not available (tests).
        private class NullConfigurationService : Core.Configuration.IConfigurationService
        {
            public System.Threading.Tasks.Task<Core.Configuration.NetAgentOptions> GetOptionsAsync(System.Threading.CancellationToken cancellationToken = default)
            {
                return System.Threading.Tasks.Task.FromResult(new Core.Configuration.NetAgentOptions());
            }

            public System.Threading.Tasks.Task<Core.Configuration.NetAgentOptions> ReloadAsync(System.Threading.CancellationToken cancellationToken = default)
            {
                return GetOptionsAsync(cancellationToken);
            }

            public System.Threading.Tasks.Task SaveAsync(Core.Configuration.NetAgentOptions options, System.Threading.CancellationToken cancellationToken = default)
            {
                // No-op configuration save in test/null implementation.
                return System.Threading.Tasks.Task.CompletedTask;
            }

            public System.Threading.Tasks.Task SaveOptionsAsync(Core.Configuration.NetAgentOptions options, System.Threading.CancellationToken cancellationToken = default)
            {
                // No-op for compatibility with new interface method
                return System.Threading.Tasks.Task.CompletedTask;
            }

            public string GetConfigFolderPath()
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ElBruno.NetAgent");
            }

            public string GetConfigFilePath()
            {
                return Path.Combine(GetConfigFolderPath(), "config.json");
            }

            public void OpenConfigFile() { /* no-op in tests */ }
            public void OpenConfigFolder() { /* no-op in tests */ }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("TrayIconService starting.");

            if (System.Windows.Application.Current?.Dispatcher == null)
            {
                _logger.LogWarning("No WPF Application available - skipping tray icon creation.");
                return Task.CompletedTask;
            }

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _menu = new ContextMenuStrip();

                var openStatus = new ToolStripMenuItem("Open Status");
                openStatus.Click += (s, e) =>
                {
                    try
                    {
                        if (_serviceProvider == null)
                        {
                            _logger.LogInformation("Service provider not available for Status window.");
                            return;
                        }

                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                var window = _serviceProvider.GetService(typeof(ElBruno.NetAgent.Views.StatusWindow)) as ElBruno.NetAgent.Views.StatusWindow;
                                if (window == null)
                                {
                                    var vm = _serviceProvider.GetService(typeof(ElBruno.NetAgent.Interfaces.IStatusViewModel)) as ElBruno.NetAgent.Interfaces.IStatusViewModel;
                                    window = new ElBruno.NetAgent.Views.StatusWindow(vm);
                                }
                                window.Show();
                            }
                            catch (Exception ex) { _logger.LogWarning(ex, "Failed to open Status window"); }
                        });
                    }
                    catch (Exception ex) { _logger.LogWarning(ex, "Open Status clicked failed"); }
                };

                var refreshNow = new ToolStripMenuItem("Refresh Now");
                refreshNow.Click += async (s, e) =>
                {
                    _logger.LogInformation("Refresh Now clicked");
                    try
                    {
                        // Trigger a background refresh: enumerate and log interfaces and sample quality for each (dry-run).
                        var list = await _inventoryService.GetInterfacesAsync(CancellationToken.None).ConfigureAwait(false);
                        foreach (var adapter in list)
                        {
                            try
                            {
                                var report = await _qualityMonitor.EvaluateAsync(adapter, CancellationToken.None).ConfigureAwait(false);
                                _logger.LogInformation("Refreshed {Name}: latency={Latency}ms loss={Loss}% score={Score}", adapter.Name, report.LatencyMs, report.PacketLossPercent, report.Score);
                            }
                            catch (Exception ex) { _logger.LogWarning(ex, "Quality sample failed for {Name}", adapter?.Name); }
                        }
                    }
                    catch (Exception ex) { _logger.LogWarning(ex, "Refresh failed"); }
                };

                var autoModeItem = new ToolStripMenuItem("Auto Mode") { CheckOnClick = true, Checked = false };
                autoModeItem.Click += (s, e) =>
                {
                    _autoMode = autoModeItem.Checked;
                    _logger.LogInformation("Auto Mode toggled: {Auto}", _autoMode);
                };



                var openLogs = new ToolStripMenuItem("Open Logs Folder");
                openLogs.Click += (s, e) => { _logger.LogInformation("Open Logs clicked"); try { _dialogService.OpenLogs(); } catch (Exception ex) { _logger.LogWarning(ex, "OpenLogs failed"); } };

                var openAppData = new ToolStripMenuItem("Open App Data Folder");
                openAppData.Click += (s, e) =>
                {
                    _logger.LogInformation("Open App Data Folder clicked");
                    try { _dialogService.OpenConfigFolder(); } catch (Exception ex) { _logger.LogWarning(ex, "OpenAppData failed"); }
                };

                var openConfig = new ToolStripMenuItem("Open Config");
                openConfig.Click += (s, e) =>
                {
                    _logger.LogInformation("Open Config clicked");
                    try { _configurationService.OpenConfigFile(); } catch (Exception ex) { _logger.LogWarning(ex, "OpenConfig failed"); }
                };

                var previewBest = new ToolStripMenuItem("Preview Best Switch (Dry-run)");
                previewBest.Click += async (s, e) =>
                {
                    try
                    {
                        var list = await _inventoryService.GetInterfacesAsync(CancellationToken.None).ConfigureAwait(false);
                        if (list == null || list.Count == 0)
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() => _notifyIcon?.ShowBalloonTip(4000, "Preview", "No interfaces detected", ToolTipIcon.Info));
                            return;
                        }

                        Core.Models.NetworkQualityReport? bestReport = null;
                        Core.Models.NetworkInterfaceInfo? bestAdapter = null;
                        var excludedReasons = new System.Collections.Generic.List<string>();

                        foreach (var adapter in list)
                        {
                            try
                            {
                                var report = await _qualityMonitor.EvaluateAsync(adapter, CancellationToken.None).ConfigureAwait(false);
                                var opts = await _configuration_service_get_options_async().ConfigureAwait(false);
                                var decision = await _decisionEngine.EvaluateAsync(report, opts, CancellationToken.None).ConfigureAwait(false);

                                if (decision?.Reason != null && decision.Reason.StartsWith("Excluded:", StringComparison.OrdinalIgnoreCase))
                                {
                                    excludedReasons.Add($"{adapter.Name}: {decision.Reason}");
                                    continue; // skip excluded adapters
                                }

                                if (bestReport == null || report.Score > bestReport.Score)
                                {
                                    bestReport = report;
                                    bestAdapter = adapter;
                                }
                            }
                            catch { /* ignore per-adapter errors */ }
                        }

                        if (bestAdapter == null)
                        {
                            var reasons = excludedReasons.Count > 0 ? string.Join("; ", excludedReasons) : "no eligible adapters";
                            var msg = $"Dry-run: no eligible adapters - {reasons}";
                            System.Windows.Application.Current.Dispatcher.Invoke(() => _notifyIcon?.ShowBalloonTip(6000, "Preview Best Switch", msg, ToolTipIcon.Info));
                            return;
                        }

                        var optsFinal = await _configuration_service_get_options_async().ConfigureAwait(false);
                        var finalDecision = await _decisionEngine.EvaluateAsync(bestReport!, optsFinal, CancellationToken.None).ConfigureAwait(false);
                        var msgFinal = $"Best: {bestAdapter!.Name} score={bestReport!.Score} -> Action={finalDecision.Action} ({finalDecision.Reason})";
                        System.Windows.Application.Current.Dispatcher.Invoke(() => _notifyIcon?.ShowBalloonTip(6000, "Preview Best Switch", msgFinal, ToolTipIcon.Info));
                    }
                    catch (Exception ex) { _logger.LogWarning(ex, "PreviewBest failed"); }
                };

                var interfacesSubmenu = new ToolStripMenuItem("Interfaces");
                interfacesSubmenu.DropDownOpening += async (s, e) =>
                {
                    interfacesSubmenu.DropDownItems.Clear();
                    try
                    {
                        var list = await _inventoryService.GetInterfacesAsync(CancellationToken.None).ConfigureAwait(false);
                        foreach (var adapter in list)
                        {
                            try
                            {
                                var report = await _qualityMonitor.EvaluateAsync(adapter, CancellationToken.None).ConfigureAwait(false);
                                var opts = await _configuration_service_get_options_async();
                                var decision = await _decisionEngine.EvaluateAsync(report, opts, CancellationToken.None).ConfigureAwait(false);

                                var display = $"{adapter.Name} ({adapter.Kind})";
                                if (decision?.Reason != null && decision.Reason.StartsWith("Excluded:", StringComparison.OrdinalIgnoreCase))
                                {
                                    display += $" - excluded: {decision.Reason.Replace("Excluded:", "").Trim()}";
                                }

                                var item = new ToolStripMenuItem(display);
                                item.Tag = adapter;
                                item.Click += async (ss, ee) =>
                                {
                                    try
                                    {
                                        var a = (Core.Models.NetworkInterfaceInfo)item.Tag;
                                        var r = await _qualityMonitor.EvaluateAsync(a, CancellationToken.None).ConfigureAwait(false);
                                        var opts2 = await _configuration_service_get_options_async();
                                        var dec = await _decisionEngine.EvaluateAsync(r, opts2, CancellationToken.None).ConfigureAwait(false);
                                        var msg = $"Adapter: {a.Name}\nLatency: {r.LatencyMs}ms Loss: {r.PacketLossPercent}% Score: {r.Score}\nDecision: {dec.Action} - {dec.Reason}";
                                        System.Windows.Application.Current.Dispatcher.Invoke(() => _notifyIcon?.ShowBalloonTip(8000, "Adapter Preview", msg, ToolTipIcon.Info));
                                    }
                                    catch (Exception ex) { _logger.LogWarning(ex, "Adapter preview failed"); }
                                };
                                interfacesSubmenu.DropDownItems.Add(item);
                            }
                            catch (Exception ex) { _logger.LogWarning(ex, "Error evaluating adapter for menu"); var item = new ToolStripMenuItem($"{adapter.Name} ({adapter.Kind}) - error"); interfacesSubmenu.DropDownItems.Add(item); }
                        }

                        if (list == null || list.Count == 0)
                            interfacesSubmenu.DropDownItems.Add(new ToolStripMenuItem("(no interfaces)"));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed populating interfaces");
                        interfacesSubmenu.DropDownItems.Add(new ToolStripMenuItem("Error"));
                    }
                };

                var exit = new ToolStripMenuItem("Exit");
                exit.Click += (s, e) =>
                {
                    _logger.LogInformation("Exit clicked.");
                    // Dispose tray resources on UI thread prior to shutting down the host/UI loop.
                    try
                    {
                        // Schedule dispose and shutdown on the UI dispatcher without blocking calling thread.
                        try
                        {
                            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                try
                                {
                                    if (_notifyIcon != null)
                                    {
                                        _notifyIcon.Visible = false;
                                        _notifyIcon.Dispose();
                                        _notifyIcon = null;
                                    }

                                    if (_menu != null)
                                    {
                                        _menu.Dispose();
                                        _menu = null;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Error disposing tray resources during Exit");
                                }

                                // Request host/application shutdown. If an IHostApplicationLifetime is available,
                                // let the generic host orchestrate stopping hosted services. Otherwise call Shutdown directly.
                                _appLifetime?.StopApplication();

                                if (_appLifetime == null)
                                {
                                    // Close any open WPF windows cleanly before shutting down.
                                    try
                                    {
                                        foreach (var w in System.Windows.Application.Current.Windows)
                                        {
                                            try { (w as System.Windows.Window)?.Close(); } catch { }
                                        }
                                    }
                                    catch { }

                                    try { System.Windows.Application.Current.Shutdown(); } catch { }
                                }
                            }));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error scheduling Exit handling on dispatcher");
                            // Best-effort stop in case dispatcher invocation failed.
                            _appLifetime?.StopApplication();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Exit click handling failed");
                        // Best-effort stop in case dispatcher invocation failed.
                        _appLifetime?.StopApplication();
                    }
                };

                // Add top-level menu items in the requested order. Interfaces submenu remains nested under "Interfaces" for previews.
                // Create Open Network Selector and Open Settings top-level items
                var openNetworkSelector = new ToolStripMenuItem("Open Network Selector");
                openNetworkSelector.Click += (s, e) =>
                {
                    try
                    {
                        if (_serviceProvider == null)
                        {
                            _logger.LogInformation("Service provider not available for Network Selector window.");
                            return;
                        }

                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                var window = _serviceProvider.GetService(typeof(ElBruno.NetAgent.Views.NetworkSelectorWindow)) as ElBruno.NetAgent.Views.NetworkSelectorWindow;
                                if (window == null)
                                {
                                    var vm = _serviceProvider.GetService(typeof(ElBruno.NetAgent.Interfaces.INetworkSelectorViewModel)) as ElBruno.NetAgent.Interfaces.INetworkSelectorViewModel;
                                    window = new ElBruno.NetAgent.Views.NetworkSelectorWindow(vm);
                                }
                                window.Show();
                            }
                            catch (Exception ex) { _logger.LogWarning(ex, "Failed to open Network Selector window"); }
                        });
                    }
                    catch (Exception ex) { _logger.LogWarning(ex, "Open Network Selector clicked failed"); }
                };

                var openSettings = new ToolStripMenuItem("Open Settings");
                openSettings.Click += (s, e) =>
                {
                    try
                    {
                        if (_serviceProvider == null)
                        {
                            _logger.LogInformation("Service provider not available for Settings window.");
                            return;
                        }

                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                var window = _serviceProvider.GetService(typeof(ElBruno.NetAgent.Views.SettingsWindow)) as ElBruno.NetAgent.Views.SettingsWindow;
                                if (window == null)
                                {
                                    var vm = _serviceProvider.GetService(typeof(ElBruno.NetAgent.Interfaces.ISettingsViewModel)) as ElBruno.NetAgent.Interfaces.ISettingsViewModel;
                                    window = new ElBruno.NetAgent.Views.SettingsWindow(vm);
                                }
                                window.Show();
                            }
                            catch (Exception ex) { _logger.LogWarning(ex, "Failed to open Settings window"); }
                        });
                    }
                    catch (Exception ex) { _logger.LogWarning(ex, "Open Settings clicked failed"); }
                };

                var openGithub = new ToolStripMenuItem("Open GitHub Repository");
                openGithub.Click += (s, e) => { try { _linkService.OpenLink("https://github.com/elbruno/ElBruno.NetAgent"); } catch (Exception ex) { _logger.LogWarning(ex, "OpenGitHub failed"); } };

                var aboutItem = new ToolStripMenuItem("About ElBruno.NetAgent");
                aboutItem.Click += (s, e) => { try { _dialogService.ShowAbout(); } catch (Exception ex) { _logger.LogWarning(ex, "ShowAbout failed"); } };

                var help = new ToolStripMenuItem("Help");
                // reuse existing openLogs/openAppData if defined; otherwise create fallback items
                try
                {
                    help.DropDownItems.AddRange(new ToolStripItem[] { openLogs ?? new ToolStripMenuItem("Open Logs Folder"), openAppData ?? new ToolStripMenuItem("Open App Data Folder"), openGithub, aboutItem });
                }
                catch
                {
                    // If openLogs/openAppData are not defined earlier, just add the items created here
                    help.DropDownItems.AddRange(new ToolStripItem[] { new ToolStripMenuItem("Open Logs Folder") { Enabled = false }, new ToolStripMenuItem("Open App Data Folder") { Enabled = false }, openGithub, aboutItem });
                }

                _menu.Items.AddRange(new ToolStripItem[] { openStatus, openNetworkSelector, openSettings, previewBest, autoModeItem, help, exit });

                if (_notifyIcon == null)
                {
                    _notifyIcon = new NotifyIcon()
                    {
                        Visible = true,
                        Text = "ElBruno.NetAgent"
                    };
                }
                else
                {
                    try { _notifyIcon.Visible = true; _notifyIcon.Text = "ElBruno.NetAgent"; } catch { }
                }

                // Prefer a repository-provided icon if present, otherwise fall back to the system icon.
                try
                {
                    var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "icons", "netagent.ico");
                    if (File.Exists(iconPath))
                    {
                        try
                        {
                            _notifyIcon.Icon = new System.Drawing.Icon(iconPath);
                        }
                        catch (Exception exIcon)
                        {
                            _logger.LogWarning(exIcon, "Failed to load icon from {IconPath}, falling back to SystemIcons.Application", iconPath);
                            _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                        }
                    }
                    else
                    {
                        _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unexpected error while selecting tray icon - falling back to SystemIcons.Application");
                    _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                }

                _notifyIcon.ContextMenuStrip = _menu;

                try
                {
                    var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "icon-placeholder.txt");
                    if (File.Exists(path))
                    {
                        var content = File.ReadAllText(path);
                        _logger.LogDebug("Icon placeholder: {Content}", content?.Split(new[] {'\r','\n'}, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed reading icon placeholder.");
                }
            });

            _logger.LogInformation("TrayIconService started and tray icon created.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("TrayIconService stopping.");

            if (System.Windows.Application.Current?.Dispatcher != null)
            {
                try
                {
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (_notifyIconAdapter != null)
                        {
                            try
                            {
                                _notifyIconAdapter.Visible = false;
                                _notifyIconAdapter.Dispose();
                            }
                            catch (Exception ex) { _logger.LogWarning(ex, "Error disposing notify icon adapter during Stop"); }
                            _notifyIconAdapter = null;
                        }
                        else if (_notifyIcon != null)
                        {
                            _notifyIcon.Visible = false;
                            _notifyIcon.Dispose();
                            _notifyIcon = null;
                        }

                        if (_menu != null)
                        {
                            _menu.Dispose();
                            _menu = null;
                        }
                    }));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error scheduling StopAsync dispose on dispatcher");
                }
            }
            return Task.CompletedTask;
        }

        // Internal helper used by tests to invoke the Exit flow without relying on UI event wiring.
        internal void InvokeExitForTests()
        {
            try
            {
                if (System.Windows.Application.Current?.Dispatcher != null)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            if (_notifyIcon != null)
                            {
                                _notifyIcon.Visible = false;
                                _notifyIcon.Dispose();
                                _notifyIcon = null;
                            }

                            if (_menu != null)
                            {
                                _menu.Dispose();
                                _menu = null;
                            }
                        }
                        catch (Exception ex) { _logger.LogWarning(ex, "Error disposing tray resources during Exit (test)"); }

                        // Request host/application shutdown.
                        _appLifetime?.StopApplication();

                        if (_appLifetime == null)
                        {
                            try
                            {
                                foreach (var w in System.Windows.Application.Current.Windows)
                                {
                                    try { (w as System.Windows.Window)?.Close(); } catch { }
                                }
                            }
                            catch { }
                            try { System.Windows.Application.Current.Shutdown(); } catch { }
                        }
                    });
                }
                else
                {
                    try
                    {
                        if (_notifyIcon != null)
                        {
                            _notifyIcon.Visible = false;
                            _notifyIcon.Dispose();
                            _notifyIcon = null;
                        }

                        if (_menu != null)
                        {
                            _menu.Dispose();
                            _menu = null;
                        }
                    }
                    catch (Exception ex) { _logger.LogWarning(ex, "Error disposing tray resources during Exit (test)"); }

                    _appLifetime?.StopApplication();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exit (test) handling failed");
                _appLifetime?.StopApplication();
            }
        }

        // Internal helper used by tests to invoke the Exit flow without relying on UI event wiring.
        internal void InvokeExitForTests_NoDispatch()
        {
            try
            {
                // Dispose tray resources directly to avoid dispatcher deadlocks in unit tests.
                try
                {
                    if (_notifyIconAdapter != null)
                    {
                        _logger.LogInformation("Disposing notify icon adapter (test)");
                        try
                        {
                            _notifyIconAdapter.Visible = false;
                            _notifyIconAdapter.Dispose();
                            _logger.LogInformation("Notify icon adapter disposed (test)");
                        }
                        catch (Exception ex) { _logger.LogWarning(ex, "Error disposing notify icon adapter during Exit (test)"); }
                        _notifyIconAdapter = null;
                        if (_notifyIcon != null)
                        {
                            try { _notifyIcon.Visible = false; _notifyIcon.Dispose(); } catch { }
                            _notifyIcon = null;
                        }
                    }
                    else if (_notifyIcon != null)
                    {
                        _logger.LogInformation("Disposing notify icon (test)");
                        _notifyIcon.Visible = false;
                        _notifyIcon.Dispose();
                        _notifyIcon = null;
                    }

                    if (_menu != null)
                    {
                        _menu.Dispose();
                        _menu = null;
                    }
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Error disposing tray resources during Exit (test)"); }

                // Request host/application shutdown.
                _appLifetime?.StopApplication();

                if (_appLifetime == null)
                {
                    try
                    {
                        if (System.Windows.Application.Current != null)
                        {
                            foreach (var w in System.Windows.Application.Current.Windows)
                            {
                                try { (w as System.Windows.Window)?.Close(); } catch { }
                            }

                            try { System.Windows.Application.Current.Shutdown(); } catch { }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exit (test) handling failed");
                _appLifetime?.StopApplication();
            }
        }

        internal bool IsNotifyIconPresentForTests()
        {
            return _notifyIcon != null || (_notifyIconAdapter != null && !_notifyIconAdapter.IsDisposed);
        }

        // Compatibility wrapper used by the updated code to centralize configuration reads
        private Task<Core.Configuration.NetAgentOptions> _configuration_service_get_options_async()
        {
            return _configurationService.GetOptionsAsync(CancellationToken.None);
        }

        public void Dispose()
        {
            try
            {
                _notifyIconAdapter?.Dispose();
            }
            catch { }
            try { _notifyIcon?.Dispose(); } catch { }
            try { _menu?.Dispose(); } catch { }
        }
    }
}
