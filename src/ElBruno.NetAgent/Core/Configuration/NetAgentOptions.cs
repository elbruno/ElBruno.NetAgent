namespace ElBruno.NetAgent.Core.Configuration;

public class NetAgentOptions
{
    // Default constants used by tests and other code.
    public const int DefaultLatencyThresholdMs = 150;
    public const double DefaultPacketLossThresholdPercent = 5.0;
    public const int DefaultCheckIntervalSeconds = 30;
    public const int DefaultFailoverDurationSeconds = 60;
    public const int DefaultFailbackCooldownSeconds = 300;
    public const int DefaultMinimumChecksBeforeSwitch = 3;
    public const double DefaultMinimumScoreDeltaToSwitch = 10.0;

    // Thresholds
    public int LatencyThresholdMs { get; set; } = DefaultLatencyThresholdMs; // ms
    public double PacketLossThresholdPercent { get; set; } = DefaultPacketLossThresholdPercent; // percent

    // Timings
    public int CheckIntervalSeconds { get; set; } = DefaultCheckIntervalSeconds; // how often to check network quality
    public int AutoModeIntervalSeconds { get; set; } = DefaultCheckIntervalSeconds; // interval for AutoMode loop
    public int FailoverDurationSeconds { get; set; } = DefaultFailoverDurationSeconds; // how long a failover decision must hold
    public int FailbackCooldownSeconds { get; set; } = DefaultFailbackCooldownSeconds; // cooldown after a failback

    // Anti-flapping
    public int MinimumChecksBeforeSwitch { get; set; } = DefaultMinimumChecksBeforeSwitch; // require this many checks before switching
    public double MinimumScoreDeltaToSwitch { get; set; } = DefaultMinimumScoreDeltaToSwitch; // score delta required to trigger switch

    // Behavior
    public bool AutoModeEnabled { get; set; } = false;
    public bool DryRunMode { get; set; } = true; // preserve dry-run default

    // Endpoints to test connectivity (hosts to ping)
    public string[] TestEndpoints { get; set; } = new[] { "8.8.8.8", "1.1.1.1" };

    // Misc
    public int MaxHistoryItems { get; set; } = 50;
}
