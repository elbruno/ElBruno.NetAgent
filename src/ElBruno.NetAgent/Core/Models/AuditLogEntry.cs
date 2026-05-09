namespace ElBruno.NetAgent.Core.Models;

/// <summary>
/// Represents an audit log entry for a simulated network controller action.
/// </summary>
public class AuditLogEntry
{
    /// <summary>
    /// The UTC timestamp when the entry was created.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// The requested action (e.g., "PreferInterface", "RestoreAutomaticMetrics").
    /// </summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>
    /// The target network interface or resource description.
    /// </summary>
    public string Target { get; init; } = string.Empty;

    /// <summary>
    /// The reason for the decision.
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// Whether this entry is from a dry-run (no real changes made).
    /// </summary>
    public bool IsDryRun { get; init; }

    /// <summary>
    /// The result or status of the simulated action.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Additional diagnostic details.
    /// </summary>
    public string? Details { get; init; }
}
