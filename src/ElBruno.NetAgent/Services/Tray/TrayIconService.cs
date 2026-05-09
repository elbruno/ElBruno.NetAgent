using System.Drawing;
using ElBruno.NetAgent.Services.Audit;
using ElBruno.NetAgent.UI.Views;
using Microsoft.Extensions.Logging;
using WinForms = System.Windows.Forms;

namespace ElBruno.NetAgent.Services.Tray;

/// <summary>
/// Manages the Windows system tray icon and context menu.
/// </summary>
public class TrayIconService : IDisposable
{
    private readonly WinForms.NotifyIcon _notifyIcon;
    private readonly ILogger<TrayIconService> _logger;
    private readonly IAuditLogService _auditLogService;
    private bool _disposed;

    public TrayIconService(
        ILogger<TrayIconService> logger,
        IAuditLogService auditLogService)
    {
        _logger = logger;
        _auditLogService = auditLogService;

        _notifyIcon = new WinForms.NotifyIcon
        {
            Visible = true,
            Text = "ElBruno.NetAgent",
            Icon = SystemIcons.Shield
        };

        _notifyIcon.ContextMenuStrip = CreateContextMenuStrip();
        _notifyIcon.DoubleClick += OnDoubleClick;

        _logger.LogInformation("Tray icon initialized");
    }

    private WinForms.ContextMenuStrip CreateContextMenuStrip()
    {
        var menuStrip = new WinForms.ContextMenuStrip();

        // Header
        menuStrip.Items.Add("ElBruno.NetAgent").Enabled = false;

        // DRY-RUN badge (prominent)
        var dryRunBadge = menuStrip.Items.Add("*** DRY-RUN MODE ***");
        dryRunBadge.Name = "DryRunBadge";
        dryRunBadge.Enabled = false;
        dryRunBadge.ForeColor = System.Drawing.Color.Red;
        dryRunBadge.Font = new System.Drawing.Font("Segoe UI", 8.25f, System.Drawing.FontStyle.Bold);

        // Status (placeholder)
        var statusItem = menuStrip.Items.Add("Status: Starting");
        statusItem.Name = "StatusItem";
        statusItem.Enabled = false;

        // Separator
        menuStrip.Items.Add("-");

        // Open Status
        menuStrip.Items.Add("Open Status", null, (s, e) =>
        {
            _logger.LogInformation("Open Status clicked");
            var window = new AuditLogViewerWindow(_auditLogService);
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.ShowDialog();
        });

        // Refresh Now
        menuStrip.Items.Add("Refresh Now", null, (s, e) =>
        {
            _logger.LogInformation("Refresh Now clicked (placeholder)");
        });

        // Separator
        menuStrip.Items.Add("-");

        // Exit
        menuStrip.Items.Add("Exit", null, (s, e) =>
        {
            _logger.LogInformation("Exit requested from tray menu");
            // Shutdown is handled by the Exit menu command in the hosted service
            Environment.Exit(0);
        });

        return menuStrip;
    }

    private void OnDoubleClick(object? sender, EventArgs e)
    {
        _logger.LogInformation("Tray icon double-clicked");
    }

    /// <summary>
    /// Updates the status text shown in the tray context menu.
    /// </summary>
    public void UpdateStatus(string status)
    {
        var statusItem = _notifyIcon.ContextMenuStrip?.Items["StatusItem"];
        if (statusItem != null)
        {
            statusItem.Text = $"Status: {status}";
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _disposed = true;

        _logger.LogInformation("Tray icon disposed");
    }
}
