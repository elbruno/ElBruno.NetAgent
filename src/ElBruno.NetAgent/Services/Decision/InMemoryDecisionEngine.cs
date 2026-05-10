using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElBruno.NetAgent.Core.Configuration;
using ElBruno.NetAgent.Core.Decision;
using ElBruno.NetAgent.Core.Models;

namespace ElBruno.NetAgent.Services.Decision
{
    /// <summary>
    /// In-memory decision engine used to score network reports and produce a decision.
    /// Purely functional and deterministic; does not interact with adapters or change system state.
    /// Respects switching rules but performs no real switching (dry-run behavior only).
    /// </summary>
    internal class InMemoryDecisionEngine : IDecisionEngine
    {
        public Task<DecisionResult> EvaluateAsync(NetworkQualityReport report, NetAgentOptions options, CancellationToken cancellationToken = default)
        {
            // Defensive checks
            report ??= new NetworkQualityReport();
            options ??= new NetAgentOptions();

            var switching = options.SwitchingRules ?? new NetAgentOptions.SwitchingRulesOptions();

            // Helper matchers (simple substring, case-insensitive)
            bool MatchesAny(string value, string[] patterns)
            {
                if (string.IsNullOrWhiteSpace(value)) return false;
                if (patterns == null || patterns.Length == 0) return false;
                foreach (var p in patterns)
                {
                    if (string.IsNullOrWhiteSpace(p)) continue;
                    if (value.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                }
                return false;
            }

            // Exclusions by kind
            if (switching.ExcludedInterfaceKinds != null && switching.ExcludedInterfaceKinds.Length > 0)
            {
                if (!string.IsNullOrWhiteSpace(report.InterfaceKind) && switching.ExcludedInterfaceKinds.Any(k => string.Equals(k, report.InterfaceKind, StringComparison.OrdinalIgnoreCase)))
                {
                    var exReason = $"Excluded: interface kind '{report.InterfaceKind}' is configured as excluded";
                    return Task.FromResult(new DecisionResult(DecisionAction.None, exReason));
                }
            }

            // Exclusions by name/id patterns
            if (switching.ExcludedInterfacePatterns != null && switching.ExcludedInterfacePatterns.Length > 0)
            {
                var nameOrId = (report.InterfaceName ?? string.Empty) + "|" + (report.InterfaceId ?? string.Empty);
                if (MatchesAny(nameOrId, switching.ExcludedInterfacePatterns))
                {
                    var exReason = $"Excluded: interface matches excluded pattern";
                    return Task.FromResult(new DecisionResult(DecisionAction.None, exReason));
                }
            }

            // Disallow certain kinds or types per allow flags (e.g., WiFi/Ethernet)
            if (!switching.AllowWiFi && string.Equals(report.InterfaceKind, "Wifi", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new DecisionResult(DecisionAction.None, "Excluded: WiFi disallowed by settings"));
            }
            if (!switching.AllowEthernet && string.Equals(report.InterfaceKind, "Ethernet", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new DecisionResult(DecisionAction.None, "Excluded: Ethernet disallowed by settings"));
            }

            // If ignoring down/unknown adapters, we can't infer operational state here; assume monitor may set score low.

            // Compute normalized score (reuse previous logic)
            double scoreNormalized;
            if (report.Score > 0)
            {
                scoreNormalized = Math.Clamp(report.Score / 100.0, 0.0, 1.0);
            }
            else
            {
                const double wPacket = 0.4, wLatency = 0.3, wJitter = 0.2, wThroughput = 0.1;
                double packetLossPercent = report.PacketLossPercent;
                double latencyMs = report.LatencyMs;
                double jitterMs = 0.0;
                double throughputKbps = 100_000.0;
                const double maxLatency = 1000.0;
                const double maxJitter = 500.0;
                const double maxThroughput = 100_000.0;

                double packetScore = 1.0 - Clamp(packetLossPercent / 100.0, 0.0, 1.0);
                double latencyScore = 1.0 - Clamp(latencyMs / maxLatency, 0.0, 1.0);
                double jitterScore = 1.0 - Clamp(jitterMs / maxJitter, 0.0, 1.0);
                double throughputScore = Clamp(throughputKbps / maxThroughput, 0.0, 1.0);

                scoreNormalized = (wPacket * packetScore) + (wLatency * latencyScore) + (wJitter * jitterScore) + (wThroughput * throughputScore);
                scoreNormalized = Clamp(scoreNormalized, 0.0, 1.0);
            }

            // Apply preferences: preferred patterns boost score slightly
            var adjustments = new System.Collections.Generic.List<string>();
            if (switching.PreferredInterfacePatterns != null && switching.PreferredInterfacePatterns.Length > 0)
            {
                var nameOrId = (report.InterfaceName ?? string.Empty) + "|" + (report.InterfaceId ?? string.Empty);
                if (MatchesAny(nameOrId, switching.PreferredInterfacePatterns))
                {
                    scoreNormalized = Math.Min(1.0, scoreNormalized + 0.10);
                    adjustments.Add("preferred interface pattern matched");
                }
            }

            if (switching.PreferUsbTethering && !string.IsNullOrWhiteSpace(report.InterfaceKind) && report.InterfaceKind.IndexOf("Usb", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                scoreNormalized = Math.Min(1.0, scoreNormalized + 0.10);
                adjustments.Add("preferred USB tethering");
            }

            // Map to actions per spec
            DecisionAction action;
            if (scoreNormalized >= 0.80) action = DecisionAction.None;
            else if (scoreNormalized >= 0.60) action = DecisionAction.NotifyUser;
            else if (scoreNormalized >= 0.35) action = DecisionAction.Throttle;
            else action = DecisionAction.SwitchAdapter;

            // Build reason with details
            string reason = $"Score={Math.Round(scoreNormalized * 100.0, 2)}% (normalized={scoreNormalized:0.00})";
            if (adjustments.Count > 0)
            {
                reason += " - " + string.Join(", ", adjustments);
            }

            // If dry-run is enabled at options level, annotate reason
            if (options.DryRunMode || (switching != null && switching.DryRunMode))
            {
                reason = "Dry-run: " + reason;
            }

            var result = new DecisionResult(action, reason);
            return Task.FromResult(result);
        }

        private static double Clamp(double v, double min, double max) => Math.Max(min, Math.Min(max, v));
    }
}
