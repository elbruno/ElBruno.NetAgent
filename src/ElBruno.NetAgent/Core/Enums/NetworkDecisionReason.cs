namespace ElBruno.NetAgent.Core.Enums;

/// <summary>
/// Reason for a network decision.
/// </summary>
public enum NetworkDecisionReason
{
    /// <summary>
    /// No decision has been made yet.
    /// </summary>
    None,

    /// <summary>
    /// Current network is healthy; stay on it.
    /// </summary>
    StayHealthyCurrent,

    /// <summary>
    /// Current network is unhealthy but a better candidate exists; switch.
    /// </summary>
    SwitchBetterCandidate,

    /// <summary>
    /// Cooldown period has not elapsed; stay.
    /// </summary>
    StayCooldown,

    /// <summary>
    /// Insufficient quality samples collected; wait.
    /// </summary>
    WaitInsufficientSamples,

    /// <summary>
    /// Score delta between candidates is too small; stay.
    /// </summary>
    StayDeltaTooSmall,

    /// <summary>
    /// A switch is already in progress; stay.
    /// </summary>
    StaySwitchInProgress,

    /// <summary>
    /// Target interface has incomplete data; stay.
    /// </summary>
    StayIncompleteTargetData,

    /// <summary>
    /// Current interface is still healthy; stay.
    /// </summary>
    StayCurrentHealthy
}
