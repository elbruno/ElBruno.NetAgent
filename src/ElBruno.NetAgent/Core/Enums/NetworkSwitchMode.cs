namespace ElBruno.NetAgent.Core.Enums;

/// <summary>
/// Specifies the execution mode for network controller operations.
/// </summary>
public enum NetworkSwitchMode
{
    /// <summary>
    /// Dry-run mode: logs intended actions without making real changes.
    /// </summary>
    DryRun,

    /// <summary>
    /// Live mode: executes actual network metric changes.
    /// </summary>
    Live
}
