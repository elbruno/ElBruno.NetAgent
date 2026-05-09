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
        private ContextMenuStrip? _menu;
        private bool _autoMode;

        public TrayIconService(ILogger<TrayIconService> logger,
            Core.Configuration.IConfigurationService configurationService,
            Core.Services.INetworkInventoryService inventoryService,
            Core.Services.INetworkQualityMonitor qualityMonitor,
            Core.Decision.IDecisionEngine decisionEngine,
            IHostApplicationLifetime? appLifetime = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appLifetime = appLifetime;
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
            _qualityMonitor = qualityMonitor ?? throw new ArgumentNullException(nameof(qualityMonitor));
            _decisionEngine = decisionEngine ?? throw new ArgumentNullException(nameof(decisionEngine));
        }

        private readonly Core.Configuration.IConfigurationService _configurationService;
        private readonly Core.Services.INetworkInventoryService _inventoryService;
        private readonly Core.Services.INetworkQualityMonitor _qualityMonitor;
        private readonly Core.Decision.IDecisionEngine _decisionEngine;

        // Back-compat constructor used by unit tests that instantiate the service directly.
        public TrayIconService(ILogger<TrayIconService> logger) : this(logger, new NullConfigurationService(), new ElBruno.NetAgent.Services.NullInventoryService(), new ElBruno.NetAgent.Services.NullQualityMonitor(), new ElBruno.NetAgent.Services.NullDecisionEngine(), null) { }

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
                openStatus.Click += (s, e) => _logger.LogInformation("Open Status clicked");

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

                var showConfig = new ToolStripMenuItem("Show Config Status");
                showConfig.Click += async (s, e) =>
                {
                    try
                    {
                        var opts = await _configurationService.GetOptionsAsync(CancellationToken.None).ConfigureAwait(false);
                        var msg = $"DryRun: {opts.DryRunMode}  AutoMode: {opts.AutoModeEnabled}  Threshold: {opts.LatencyThresholdMs}ms";
                        System.Windows.Application.Current.Dispatcher.Invoke(() => _notifyIcon?.ShowBalloonTip(5000, "Config Status", msg, ToolTipIcon.Info));
                    }
                    catch (Exception ex) { _logger.LogWarning(ex, "ShowConfig failed"); }
                };

                var openLogs = new ToolStripMenuItem("Open Logs");
                openLogs.Click += (s, e) => _logger.LogInformation("Open Logs clicked");

                var openAppData = new ToolStripMenuItem("Open App Data Folder");
                openAppData.Click += (s, e) =>
                {
                    _logger.LogInformation("Open App Data Folder clicked");
                    try { _configurationService.OpenConfigFolder(); } catch (Exception ex) { _logger.LogWarning(ex, "OpenAppData failed"); }
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

                        foreach (var adapter in list)
                        {
                            try
                            {
                                var report = await _qualityMonitor.EvaluateAsync(adapter, CancellationToken.None).ConfigureAwait(false);
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
                            System.Windows.Application.Current.Dispatcher.Invoke(() => _notifyIcon?.ShowBalloonTip(4000, "Preview", "No suitable adapter found", ToolTipIcon.Info));
                            return;
                        }

                        var opts = await _configurationService.GetOptionsAsync(CancellationToken.None).ConfigureAwait(false);
                        var decision = await _decisionEngine.EvaluateAsync(bestReport!, opts, CancellationToken.None).ConfigureAwait(false);
                        var msg = $"Best: {bestAdapter!.Name} score={bestReport!.Score} -> Action={decision.Action} ({decision.Reason})";
                        System.Windows.Application.Current.Dispatcher.Invoke(() => _notifyIcon?.ShowBalloonTip(6000, "Preview Best Switch", msg, ToolTipIcon.Info));
                    }
                    catch (Exception ex) { _logger.LogWarning(ex, "PreviewBest failed"); }
                };

                var interfacesMenu = new ToolStripMenuItem("Interfaces");
                interfacesMenu.DropDownOpening += async (s, e) =>
                {
                    interfacesMenu.DropDownItems.Clear();
                    try
                    {
                        var list = await _inventoryService.GetInterfacesAsync(CancellationToken.None).ConfigureAwait(false);
                        foreach (var adapter in list)
                        {
                            var item = new ToolStripMenuItem($"{adapter.Name} ({adapter.Kind})");
                            item.Tag = adapter;
                            item.Click += async (ss, ee) =>
                            {
                                try
                                {
                                    var a = (Core.Models.NetworkInterfaceInfo)item.Tag;
                                    var report = await _qualityMonitor.EvaluateAsync(a, CancellationToken.None).ConfigureAwait(false);
                                    var opts = await _configurationService.GetOptionsAsync(CancellationToken.None).ConfigureAwait(false);
                                    var decision = await _decisionEngine.EvaluateAsync(report, opts, CancellationToken.None).ConfigureAwait(false);
                                    var msg = $"Adapter: {a.Name}\nLatency: {report.LatencyMs}ms Loss: {report.PacketLossPercent}% Score: {report.Score}\nDecision: {decision.Action} - {decision.Reason}";
                                    System.Windows.Application.Current.Dispatcher.Invoke(() => _notifyIcon?.ShowBalloonTip(8000, "Adapter Preview", msg, ToolTipIcon.Info));
                                }
                                catch (Exception ex) { _logger.LogWarning(ex, "Adapter preview failed"); }
                            };
                            interfacesMenu.DropDownItems.Add(item);
                        }

                        if (list == null || list.Count == 0)
                            interfacesMenu.DropDownItems.Add(new ToolStripMenuItem("(no interfaces)"));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed populating interfaces");
                        interfacesMenu.DropDownItems.Add(new ToolStripMenuItem("Error"));
                    }
                };

                var exit = new ToolStripMenuItem("Exit");
                exit.Click += (s, e) =>
                {
                    _logger.LogInformation("Exit clicked.");
                    _appLifetime?.StopApplication();
                    if (_appLifetime == null)
                    {
                        System.Windows.Application.Current.Shutdown();
                    }
                };

                _menu.Items.AddRange(new ToolStripItem[] { openStatus, refreshNow, autoModeItem, showConfig, interfacesMenu, previewBest, openLogs, openAppData, openConfig, exit });

                _notifyIcon = new NotifyIcon()
                {
                    Icon = System.Drawing.SystemIcons.Application,
                    Visible = true,
                    Text = "ElBruno.NetAgent"
                };

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
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
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
                });
            }
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _notifyIcon?.Dispose();
            _menu?.Dispose();
        }
    }
}
