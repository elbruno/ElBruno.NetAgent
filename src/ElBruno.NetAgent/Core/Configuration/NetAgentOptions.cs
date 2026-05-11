namespace ElBruno.NetAgent.Core.Configuration;

public class NetAgentOptions
{
    /// <summary>
    /// Options that drive automatic switching behavior and UI toggles.
    /// New properties were added for UI binding while keeping legacy names for compatibility.
    /// </summary>
    public class SwitchingRulesOptions
    {
        /// <summary>When true the service will not perform real network changes.</summary>
        public bool DryRunMode { get; set; } = true;

        /// <summary>Enable periodic automatic evaluation and potential switching.</summary>
        public bool AutoModeEnabled { get; set; } = false;

        /// <summary>Interval (seconds) between automatic evaluations.</summary>
        public int AutoModeIntervalSeconds { get; set; } = DefaultCheckIntervalSeconds;

        /// <summary>Timestamp (seconds since epoch or marker) to pause auto-switching until.</summary>
        public int PauseAutoSwitchUntilSeconds { get; set; } = 0;

        /// <summary>Duration in seconds to pause auto-switching once paused.</summary>
        public int PauseAutoSwitchDurationSeconds { get; set; } = 0;

        // Score thresholds
        public double MinimumQualityScore { get; set; } = 0.0;
        public double MinimumScoreImprovement { get; set; } = 0.0;

        // New names expected by UI - proxy to legacy properties to maintain compatibility
        public double MinimumQualityScoreRequired { get => MinimumQualityScore; set => MinimumQualityScore = value; }
        public double MinimumScoreImprovementRequired { get => MinimumScoreImprovement; set => MinimumScoreImprovement = value; }

        /// <summary>Preferred interface name patterns (comma separated in UI).</summary>
        public string[] PreferredInterfacePatterns { get; set; } = new string[0];

        /// <summary>Interface name patterns to exclude from consideration.</summary>
        public string[] ExcludedInterfacePatterns { get; set; } = new string[0];

        /// <summary>Interface kinds to exclude (e.g., Loopback, Vpn).</summary>
        public string[] ExcludedInterfaceKinds { get; set; } = new string[] { "Loopback", "Vpn" };

        /// <summary>Prefer USB-tethered interfaces when selecting.</summary>
        public bool PreferUsbTethering { get; set; } = false;

        // Legacy and UI-friendly names for transport allowances
        public bool AllowWiFi { get; set; } = true;
        public bool AllowEthernet { get; set; } = true;
        // UI expects AllowWifi (different casing); proxy to AllowWiFi
        public bool AllowWifi { get => AllowWiFi; set => AllowWiFi = value; }

        /// <summary>Ignore virtual adapters when evaluating interfaces.</summary>
        public bool IgnoreVirtualAdapters { get; set; } = true;

        /// <summary>Flag to ignore loopback adapters specifically (UI-bound).</summary>
        public bool IgnoreLoopbackAdapters { get; set; } = true;

        /// <summary>Flag to ignore VPN adapters specifically (UI-bound).</summary>
        public bool IgnoreVpnAdapters { get; set; } = true;

        /// <summary>Ignore adapters that are down or in unknown state.</summary>
        public bool IgnoreDownOrUnknownAdapters { get; set; } = true;

        // Backwards-compatible alias expected by some UI code
        public bool IgnoreDownUnknownAdapters { get => IgnoreDownOrUnknownAdapters; set => IgnoreDownOrUnknownAdapters = value; }
    }

    public SwitchingRulesOptions? SwitchingRules { get; set; } = new SwitchingRulesOptions();

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
