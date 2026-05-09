using ElBruno.NetAgent.Core.Enums;
using ElBruno.NetAgent.Core.Models;

namespace ElBruno.NetAgent.Core.Models;

/// <summary>
/// Context provided to the decision engine for evaluating a network switch.
/// </summary>
public class NetworkDecisionContext
{
    /// <summary>
    /// The current active interface.
    /// </summary>
    public NetworkInterfaceInfo CurrentInterface { get; init; } = new();

    /// <summary>
    /// The current interface's latest quality snapshot.
    /// </summary>
    public NetworkQualitySnapshot? CurrentQuality { get; init; }

    /// <summary>
    /// The candidate interface to potentially switch to.
    /// </summary>
    public NetworkInterfaceInfo TargetInterface { get; init; } = new();

    /// <summary>
    /// The target interface's latest quality snapshot.
    /// </summary>
    public NetworkQualitySnapshot? TargetQuality { get; init; }

    /// <summary>
    /// Rolling history of current interface quality snapshots.
    /// </summary>
    public IReadOnlyList<NetworkQualitySnapshot> CurrentHistory { get; init; } = Array.Empty<NetworkQualitySnapshot>();

    /// <summary>
    /// Rolling history of target interface quality snapshots.
    /// </summary>
    public IReadOnlyList<NetworkQualitySnapshot> TargetHistory { get; init; } = Array.Empty<NetworkQualitySnapshot>();

    /// <summary>
    /// Whether automatic mode is enabled.
    /// </summary>
    public bool AutoModeEnabled { get; init; }

    /// <summary>
    /// Whether dry-run mode is enabled (log only, no actual switch).
    /// </summary>
    public bool DryRunMode { get; init; }

    /// <summary>
    /// Minimum checks before considering a switch.
    /// </summary>
    public int MinimumChecksBeforeSwitch { get; init; } = 3;

    /// <summary>
    /// Minimum score delta to trigger a switch.
    /// </summary>
    public int MinimumScoreDeltaToSwitch { get; init; } = 20;

    /// <summary>
    /// Cooldown period in seconds after last switch.
    /// </summary>
    public int FailbackCooldownSeconds { get; init; } = 120;

    /// <summary>
    /// Time of the last switch, or null if none.
    /// </summary>
    public DateTime? LastSwitchTime { get; init; }

    /// <summary>
    /// Whether a switch is currently in progress.
    /// </summary>
    public bool IsSwitchInProgress { get; init; }
}
