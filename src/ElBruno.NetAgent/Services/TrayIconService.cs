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

        public TrayIconService(ILogger<TrayIconService> logger, IHostApplicationLifetime? appLifetime = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appLifetime = appLifetime;
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
                refreshNow.Click += (s, e) => _logger.LogInformation("Refresh Now clicked");

                var autoModeItem = new ToolStripMenuItem("Auto Mode") { CheckOnClick = true, Checked = false };
                autoModeItem.Click += (s, e) =>
                {
                    _autoMode = autoModeItem.Checked;
                    _logger.LogInformation("Auto Mode toggled: {Auto}", _autoMode);
                };

                var openLogs = new ToolStripMenuItem("Open Logs");
                openLogs.Click += (s, e) => _logger.LogInformation("Open Logs clicked");

                var openConfig = new ToolStripMenuItem("Open Config");
                openConfig.Click += (s, e) => _logger.LogInformation("Open Config clicked");

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

                _menu.Items.AddRange(new ToolStripItem[] { openStatus, refreshNow, autoModeItem, openLogs, openConfig, exit });

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
