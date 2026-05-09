namespace ElBruno.NetAgent.Core.Models;

/// <summary>
/// Result of pinging a single endpoint.
/// </summary>
public class EndpointPingResult
{
    /// <summary>
    /// The endpoint that was pinged.
    /// </summary>
    public string Endpoint { get; init; } = string.Empty;

    /// <summary>
    /// Whether the ping succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Latency in milliseconds, or 0 if the ping failed.
    /// </summary>
    public long LatencyMs { get; init; }

    /// <summary>
    /// When the ping was performed.
    /// </summary>
    public DateTime Timestamp { get; init; }
}
