using System;
using System.Collections.Generic;
using System.Linq;
using ElBruno.NetAgent.Core.Models;
using ElBruno.NetAgent.Core.Enums;

namespace ElBruno.NetAgent.Core.Configuration
{
    public enum ExcludedInterfaceKind
    {
        Virtual,
        Loopback,
        VPN,
        Ethernet,
        Wireless,
        UsbTethering
    }

    public class SwitchingRules
    {
        public bool DryRunMode { get; set; } = true;
        public bool AutoModeEnabled { get; set; } = false;
        public int AutoModeIntervalSeconds { get; set; } = NetAgentOptions.DefaultCheckIntervalSeconds;

        // Pause duration in seconds (nullable semantics can be added later)
        public int PauseDurationSeconds { get; set; } = 0;

        public double MinQualityScore { get; set; } = 0.0; // 0..100
        public double MinScoreImprovement { get; set; } = 5.0; // points

        // Patterns (newline-separated in UI); interpreted as case-insensitive substring matches
        public List<string> PreferredInterfacePatterns { get; set; } = new List<string>();
        public List<string> ExcludedInterfacePatterns { get; set; } = new List<string>();

        public List<ExcludedInterfaceKind> ExcludedInterfaceKinds { get; set; } = new List<ExcludedInterfaceKind> { ExcludedInterfaceKind.Virtual, ExcludedInterfaceKind.Loopback, ExcludedInterfaceKind.VPN };

        public bool PreferUsbTethering { get; set; } = false;
        public bool AllowWifi { get; set; } = true;
        public bool AllowEthernet { get; set; } = true;
        public bool IgnoreVirtualAdapters { get; set; } = true;
        public bool IgnoreDownAdapters { get; set; } = true;

        // Helper to determine if an adapter should be excluded by rules
        public bool IsAdapterExcluded(NetworkInterfaceInfo adapter, out string reason)
        {
            reason = string.Empty;
            if (adapter == null) return false;

            // Ignore down adapters
            if (IgnoreDownAdapters && !adapter.IsUp)
            {
                reason = "Adapter is down";
                return true;
            }

            // Map adapter.Kind to our ExcludedInterfaceKind semantics
            var kind = adapter.Kind;

            if (IgnoreVirtualAdapters && (kind == NetworkAdapterKind.Virtual || kind == NetworkAdapterKind.Loopback || kind == NetworkAdapterKind.Vpn))
            {
                reason = "Virtual/loopback/VPN adapter ignored";
                return true;
            }

            if (ExcludedInterfaceKinds != null && ExcludedInterfaceKinds.Any())
            {
                foreach (var ek in ExcludedInterfaceKinds)
                {
                    if (ek == ExcludedInterfaceKind.Virtual && kind == NetworkAdapterKind.Virtual) { reason = "Excluded kind: Virtual"; return true; }
                    if (ek == ExcludedInterfaceKind.Loopback && kind == NetworkAdapterKind.Loopback) { reason = "Excluded kind: Loopback"; return true; }
                    if (ek == ExcludedInterfaceKind.VPN && kind == NetworkAdapterKind.Vpn) { reason = "Excluded kind: VPN"; return true; }
                    if (ek == ExcludedInterfaceKind.Ethernet && kind == NetworkAdapterKind.Ethernet) { reason = "Excluded kind: Ethernet"; return true; }
                    if (ek == ExcludedInterfaceKind.Wireless && kind == NetworkAdapterKind.WiFi) { reason = "Excluded kind: Wireless"; return true; }
                    if (ek == ExcludedInterfaceKind.UsbTethering && kind == NetworkAdapterKind.UsbTethering) { reason = "Excluded kind: UsbTethering"; return true; }
                }
            }

            // Pattern based exclusions
            if (ExcludedInterfacePatterns != null && ExcludedInterfacePatterns.Any())
            {
                var text = (adapter.Name + " " + adapter.Description).ToLowerInvariant();
                foreach (var pat in ExcludedInterfacePatterns)
                {
                    if (string.IsNullOrWhiteSpace(pat)) continue;
                    if (text.Contains(pat.Trim().ToLowerInvariant())) { reason = $"Matched excluded pattern: {pat}"; return true; }
                }
            }

            return false;
        }
    }
}
