namespace ElBruno.NetAgent.Core.Models;

/// <summary>
/// Configuration options for the network agent.
/// </summary>
public class NetAgentOptions
{
    /// <summary>
    /// Whether automatic network switching is enabled.
    /// Default: false (first launch should be safe).
    /// </summary>
    public bool AutoModeEnabled { get; set; } = false;

    /// <summary>
    /// Whether to log intended switch actions without executing them.
    /// Default: true (safe default for AI-generated implementation).
    /// </summary>
    public bool DryRunMode { get; set; } = true;

    /// <summary>
    /// How often to measure network quality (in seconds).
    /// Default: 5.
    /// </summary>
    public int CheckIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Timeout per ping request (in milliseconds).
    /// Default: 1000.
    /// </summary>
    public int PingTimeoutMs { get; set; } = 1000;

    /// <summary>
    /// Latency threshold in milliseconds. Above this value, connection is considered degraded.
    /// Default: 180.
    /// </summary>
    public int LatencyThresholdMs { get; set; } = 180;

    /// <summary>
    /// Packet loss threshold percentage. Above this value, connection is considered poor.
    /// Default: 20.
    /// </summary>
    public int PacketLossThresholdPercent { get; set; } = 20;

    /// <summary>
    /// How long the current network must remain unhealthy before a failover switch is considered (in seconds).
    /// Default: 20.
    /// </summary>
    public int FailoverDurationSeconds { get; set; } = 20;

    /// <summary>
    /// Minimum time after a switch before another automatic switch is allowed (in seconds).
    /// Default: 120.
    /// </summary>
    public int FailbackCooldownSeconds { get; set; } = 120;

    /// <summary>
    /// Minimum number of quality samples required before switching is considered.
    /// Default: 3.
    /// </summary>
    public int MinimumChecksBeforeSwitch { get; set; } = 3;

    /// <summary>
    /// Minimum score difference between candidate and current interface to trigger a switch.
    /// Default: 20.
    /// </summary>
    public int MinimumScoreDeltaToSwitch { get; set; } = 20;

    /// <summary>
    /// Preferred adapter kinds in priority order.
    /// Default: UsbTethering, Ethernet, WiFi.
    /// </summary>
    public List<string> PreferredAdapterKinds { get; set; } = new() { "UsbTethering", "Ethernet", "WiFi" };

    /// <summary>
    /// Adapter descriptions containing these values will not be preferred automatically.
    /// Default: Loopback, VirtualBox, Hyper-V, VMware.
    /// </summary>
    public List<string> IgnoredAdapterNameContains { get; set; } = new() { "Loopback", "VirtualBox", "Hyper-V", "VMware" };

    /// <summary>
    /// Endpoints used for latency checks.
    /// Default: 1.1.1.1, 8.8.8.8, 9.9.9.9.
    /// </summary>
    public List<string> PingEndpoints { get; set; } = new() { "1.1.1.1", "8.8.8.8", "9.9.9.9" };

    /// <summary>
    /// Minimum log level.
    /// Default: Information.
    /// </summary>
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// Whether live network execution is allowed.
    /// Default: false — live execution is intentionally blocked until explicitly enabled.
    /// This is the hard safety gate: even if DryRunMode is false, live operations
    /// will NOT execute unless this flag is true.
    /// </summary>
    public bool LiveModeAllowed { get; set; } = false;

    /// <summary>
    /// Whether to show toast notifications.
    /// Default: true.
    /// </summary>
    public bool ShowNotifications { get; set; } = true;

    /// <summary>
    /// Whether to start the app minimized to the system tray.
    /// Default: true.
    /// </summary>
    public bool StartMinimizedToTray { get; set; } = true;
}
