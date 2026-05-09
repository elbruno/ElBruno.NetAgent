using ElBruno.NetAgent.Core.Enums;
using ElBruno.NetAgent.Core.Models;
using ElBruno.NetAgent.Infrastructure.Windows;
using ElBruno.NetAgent.Services.Audit;
using Microsoft.Extensions.Logging;

namespace ElBruno.NetAgent.Services.Control;

/// <summary>
/// Windows-specific network controller that adjusts interface metrics.
/// Defaults to dry-run mode — no real network changes are made.
/// </summary>
public class WindowsNetworkController : INetworkController
{
    private readonly ILogger<WindowsNetworkController> _logger;
    private readonly IWindowsAdminService _adminService;
    private readonly IAuditLogService _auditLog;
    private readonly NetworkSwitchMode _mode;
    private readonly bool _liveModeAllowed;

    /// <summary>
    /// Creates a new WindowsNetworkController.
    /// </summary>
    /// <param name="logger">Logger for recording intended actions.</param>
    /// <param name="adminService">Admin elevation detector.</param>
    /// <param name="auditLog">Audit log service for recording simulated actions.</param>
    /// <param name="mode">Execution mode — defaults to DryRun.</param>
    public WindowsNetworkController(
        ILogger<WindowsNetworkController> logger,
        IWindowsAdminService adminService,
        IAuditLogService auditLog,
        NetworkSwitchMode mode = NetworkSwitchMode.DryRun)
        : this(logger, adminService, auditLog, mode, liveModeAllowed: false)
    {
    }

    /// <summary>
    /// Creates a new WindowsNetworkController with explicit live mode control.
    /// </summary>
    /// <param name="logger">Logger for recording intended actions.</param>
    /// <param name="adminService">Admin elevation detector.</param>
    /// <param name="auditLog">Audit log service for recording simulated actions.</param>
    /// <param name="mode">Execution mode — defaults to DryRun.</param>
    /// <param name="liveModeAllowed">Whether live network execution is permitted. Default: false.</param>
    public WindowsNetworkController(
        ILogger<WindowsNetworkController> logger,
        IWindowsAdminService adminService,
        IAuditLogService auditLog,
        NetworkSwitchMode mode,
        bool liveModeAllowed)
    {
        _logger = logger;
        _adminService = adminService;
        _auditLog = auditLog;
        _liveModeAllowed = liveModeAllowed;

        // Phase 10: Hard safety gate — if live mode is not explicitly allowed, force dry-run
        _mode = (_liveModeAllowed && mode == NetworkSwitchMode.Live)
            ? NetworkSwitchMode.Live
            : NetworkSwitchMode.DryRun;
    }

    public async Task<NetworkSwitchResult> PreferInterfaceAsync(string interfaceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(interfaceId))
        {
            return new NetworkSwitchResult
            {
                Succeeded = false,
                Message = "Interface ID is null or empty.",
                IsDryRun = _mode == NetworkSwitchMode.DryRun,
                Diagnostics = "No interface specified."
            };
        }

        // Safety: always log admin status
        if (!_adminService.IsAdministrator)
        {
            _logger.LogWarning(
                "[DRY-RUN] Current process is not running as administrator. " +
                "Real metric changes would require elevation. No changes made.");
        }

        if (_mode == NetworkSwitchMode.DryRun)
        {
            // DRY-RUN: Log intended action only — no real network changes
            _logger.LogInformation(
                "[DRY-RUN] Would set interface {InterfaceId} to preferred metric. " +
                "No real changes were made.", interfaceId);

            _logger.LogInformation(
                "[DRY-RUN] Intended command: Set-NetIPInterface '{0}' -InterfaceMetric 50", interfaceId);

            _logger.LogInformation(
                "[DRY-RUN] Intended command: Get-NetIPInterface | Where-Object {{ $_.InterfaceMetric -ne $null }} | Format-Table");

            // Create audit log entry
            await _auditLog.AddEntryAsync(new AuditLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Action = "PreferInterface",
                Target = interfaceId,
                Reason = "NetworkDecisionEngine determined this interface has better quality",
                IsDryRun = true,
                Status = "Succeeded",
                Details = $"Would set interface '{interfaceId}' to metric 50. No real changes made."
            });

            return new NetworkSwitchResult
            {
                Succeeded = true,
                Message = $"Dry-run: Would prefer interface '{interfaceId}' by setting metric to 50.",
                InterfaceId = interfaceId,
                IsDryRun = true,
                Diagnostics = "No real changes made. Set mode to Live to execute."
            };
        }

        // LIVE mode placeholder — not implemented yet
        _logger.LogWarning(
            "Live mode is not yet implemented. Switching to dry-run for safety.");

        return new NetworkSwitchResult
        {
            Succeeded = false,
            Message = "Live mode is not yet implemented.",
            InterfaceId = interfaceId,
            IsDryRun = false,
            Diagnostics = "Live mode requires future implementation."
        };
    }

    public async Task<NetworkSwitchResult> RestoreAutomaticMetricsAsync(CancellationToken cancellationToken = default)
    {
        if (!_adminService.IsAdministrator)
        {
            _logger.LogWarning(
                "[DRY-RUN] Current process is not running as administrator. " +
                "Restore would require elevation. No changes made.");
        }

        if (_mode == NetworkSwitchMode.DryRun)
        {
            // DRY-RUN: Log intended action only — no real network changes
            _logger.LogInformation(
                "[DRY-RUN] Would restore automatic metrics for all interfaces. " +
                "No real changes were made.");

            _logger.LogInformation(
                "[DRY-RUN] Intended command: Set-NetIPInterface -AutomaticMetric enabled");

            _logger.LogInformation(
                "[DRY-RUN] Intended command: Get-NetIPInterface | Format-Table InterfaceDescription, InterfaceMetric, AutomaticMetric");

            // Create audit log entry
            await _auditLog.AddEntryAsync(new AuditLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Action = "RestoreAutomaticMetrics",
                Target = "AllInterfaces",
                Reason = "NetworkDecisionEngine determined automatic metrics should be restored",
                IsDryRun = true,
                Status = "Succeeded",
                Details = "Would restore automatic metrics for all interfaces. No real changes made."
            });

            return new NetworkSwitchResult
            {
                Succeeded = true,
                Message = "Dry-run: Would restore automatic metrics for all interfaces.",
                IsDryRun = true,
                Diagnostics = "No real changes made. Set mode to Live to execute."
            };
        }

        // LIVE mode placeholder — not implemented yet
        _logger.LogWarning(
            "Live mode is not yet implemented. Switching to dry-run for safety.");

        return new NetworkSwitchResult
        {
            Succeeded = false,
            Message = "Live mode is not yet implemented.",
            IsDryRun = false,
            Diagnostics = "Live mode requires future implementation."
        };
    }
}
