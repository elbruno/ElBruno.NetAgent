using ElBruno.NetAgent.Core.Enums;

namespace ElBruno.NetAgent.Core.Models;

/// <summary>
/// Quality snapshot for a single network interface at a point in time.
/// </summary>
public class NetworkQualitySnapshot
{
    /// <summary>
    /// The interface this snapshot belongs to.
    /// </summary>
    public string InterfaceId { get; init; } = string.Empty;

    /// <summary>
    /// The interface name.
    /// </summary>
    public string InterfaceName { get; init; } = string.Empty;

    /// <summary>
    /// Average latency across all endpoints in milliseconds.
    /// </summary>
    public double AverageLatencyMs { get; init; }

    /// <summary>
    /// Minimum latency across all endpoints in milliseconds.
    /// </summary>
    public double MinLatencyMs { get; init; }

    /// <summary>
    /// Maximum latency across all endpoints in milliseconds.
    /// </summary>
    public double MaxLatencyMs { get; init; }

    /// <summary>
    /// Packet loss percentage (0-100).
    /// </summary>
    public double PacketLossPercent { get; init; }

    /// <summary>
    /// Deterministic quality score (0-100).
    /// </summary>
    public double QualityScore { get; init; }

    /// <summary>
    /// Assessed quality level.
    /// </summary>
    public NetworkQualityLevel QualityLevel { get; init; }

    /// <summary>
    /// Individual endpoint results.
    /// </summary>
    public IReadOnlyList<EndpointPingResult> EndpointResults { get; init; } = Array.Empty<EndpointPingResult>();

    /// <summary>
    /// When this snapshot was taken.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Rolling history of recent snapshots for this interface.
    /// </summary>
    public IReadOnlyList<NetworkQualitySnapshot> RollingHistory { get; init; } = Array.Empty<NetworkQualitySnapshot>();
}
