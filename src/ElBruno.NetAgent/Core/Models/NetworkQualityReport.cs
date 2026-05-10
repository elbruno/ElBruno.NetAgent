namespace ElBruno.NetAgent.Core.Models
{
    /// <summary>
    /// Simple, serializable network quality report produced by the monitor.
    /// </summary>
    public class NetworkQualityReport
    {
        public string InterfaceId { get; set; } = string.Empty;
        public string InterfaceName { get; set; } = string.Empty;
        public string InterfaceKind { get; set; } = string.Empty;
        public int LatencyMs { get; set; }
        public double PacketLossPercent { get; set; }
        public double Score { get; set; }

        /// <summary>
        /// Convenience: report healthy using the project's default thresholds.
        /// This uses the conservative defaults and is suitable for tests and summaries.
        /// </summary>
        public bool IsHealthy => LatencyMs <= Core.Configuration.NetAgentOptions.DefaultLatencyThresholdMs
                                 && PacketLossPercent <= Core.Configuration.NetAgentOptions.DefaultPacketLossThresholdPercent;
    }
}
