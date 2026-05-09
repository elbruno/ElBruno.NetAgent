namespace ElBruno.NetAgent.Core.Models;

/// <summary>
/// Represents the result of a network controller operation.
/// </summary>
public class NetworkSwitchResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// A human-readable message describing the result.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// The interface ID that was targeted, if applicable.
    /// </summary>
    public string? InterfaceId { get; init; }

    /// <summary>
    /// The previous metric value before the operation, if applicable.
    /// </summary>
    public int? PreviousMetric { get; init; }

    /// <summary>
    /// The new metric value after the operation, if applicable.
    /// </summary>
    public int? NewMetric { get; init; }

    /// <summary>
    /// Whether this result is from a dry-run (no real changes made).
    /// </summary>
    public bool IsDryRun { get; init; }

    /// <summary>
    /// Additional diagnostic information.
    /// </summary>
    public string? Diagnostics { get; init; }
}
