using ElBruno.NetAgent.Core.Enums;

namespace ElBruno.NetAgent.Core.Models;

/// <summary>
/// Result of a network decision evaluation.
/// </summary>
public class NetworkDecision
{
    /// <summary>
    /// Whether to switch to the target interface.
    /// </summary>
    public bool ShouldSwitch { get; init; }

    /// <summary>
    /// The reason for this decision.
    /// </summary>
    public NetworkDecisionReason Reason { get; init; }

    /// <summary>
    /// Human-readable explanation of the decision.
    /// </summary>
    public string ReasonText { get; init; } = string.Empty;

    /// <summary>
    /// The target interface ID to switch to, or empty if staying.
    /// </summary>
    public string TargetInterfaceId { get; init; } = string.Empty;

    /// <summary>
    /// The current interface score before the decision.
    /// </summary>
    public double CurrentScore { get; init; }

    /// <summary>
    /// The target interface score before the decision.
    /// </summary>
    public double TargetScore { get; init; }
}
