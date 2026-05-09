using ElBruno.NetAgent.Core.Models;

namespace ElBruno.NetAgent.Services.Control;

/// <summary>
/// Service responsible for applying network preference changes safely.
/// </summary>
public interface INetworkController
{
    /// <summary>
    /// Attempts to prefer the specified interface by adjusting its route metric.
    /// In dry-run mode, logs the intended action without making real changes.
    /// </summary>
    Task<NetworkSwitchResult> PreferInterfaceAsync(string interfaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores automatic metrics for all interfaces.
    /// In dry-run mode, logs the intended action without making real changes.
    /// </summary>
    Task<NetworkSwitchResult> RestoreAutomaticMetricsAsync(CancellationToken cancellationToken = default);
}
