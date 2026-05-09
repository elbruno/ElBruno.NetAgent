namespace ElBruno.NetAgent.Core.Models;

/// <summary>
/// Operational state of a network interface.
/// </summary>
public enum NetworkOperationalState
{
    Unknown,
    Up,
    Down,
    Testing
}

/// <summary>
/// Represents a detected network interface with classification metadata.
/// </summary>
public class NetworkInterfaceInfo
{
    /// <summary>
    /// Unique identifier for the interface.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable name of the interface.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Description provided by the OS for the interface.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// The classified adapter kind.
    /// </summary>
    public Core.Enums.NetworkAdapterKind AdapterKind { get; init; }

    /// <summary>
    /// Whether the interface is currently connected/active.
    /// </summary>
    public NetworkOperationalState OperationalState { get; init; }

    /// <summary>
    /// The MAC address of the interface, if available.
    /// </summary>
    public string? MacAddress { get; init; }

    /// <summary>
    /// The speed of the interface in bytes per second.
    /// </summary>
    public long SpeedBytesPerSecond { get; init; }

    /// <summary>
    /// Whether this interface is considered a valid candidate for network switching.
    /// </summary>
    public bool IsCandidate =>
        OperationalState == NetworkOperationalState.Up &&
        AdapterKind != Core.Enums.NetworkAdapterKind.Loopback &&
        AdapterKind != Core.Enums.NetworkAdapterKind.Virtual;
}
