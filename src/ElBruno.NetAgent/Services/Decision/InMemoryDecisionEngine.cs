using System;
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
    /// </summary>
    internal class InMemoryDecisionEngine : IDecisionEngine
    {
        public Task<DecisionResult> EvaluateAsync(NetworkQualityReport report, NetAgentOptions options, CancellationToken cancellationToken = default)
        {
            // Defensive checks
            report ??= new NetworkQualityReport();
            options ??= new NetAgentOptions();

            // If a precomputed score exists (0-100), prefer it
            double scoreNormalized;
            if (report.Score > 0)
            {
                scoreNormalized = Math.Clamp(report.Score / 100.0, 0.0, 1.0);
            }
            else
            {
                // Compute using available metrics. Missing metrics are treated as neutral (not punitive).
                // Weights: packetLoss 0.4, latency 0.3, jitter 0.2, throughput 0.1
                const double wPacket = 0.4, wLatency = 0.3, wJitter = 0.2, wThroughput = 0.1;

                double packetLossPercent = report.PacketLossPercent; // 0..100
                double latencyMs = report.LatencyMs; // ms

                // The model doesn't currently provide jitter or throughput; assume neutral defaults.
                double jitterMs = 0.0; // neutral (best)
                double throughputKbps = 100_000.0; // neutral (very good)

                // Normalization ranges (conservative):
                const double maxLatency = 1000.0; // ms
                const double maxJitter = 500.0; // ms
                const double maxThroughput = 100_000.0; // kbps

                double packetScore = 1.0 - Clamp(packetLossPercent / 100.0, 0.0, 1.0); // 1.0 == 0% loss
                double latencyScore = 1.0 - Clamp(latencyMs / maxLatency, 0.0, 1.0); // 1.0 == 0ms
                double jitterScore = 1.0 - Clamp(jitterMs / maxJitter, 0.0, 1.0); // 1.0 == 0ms
                double throughputScore = Clamp(throughputKbps / maxThroughput, 0.0, 1.0); // 1.0 == >=maxThroughput

                scoreNormalized = (wPacket * packetScore) + (wLatency * latencyScore) + (wJitter * jitterScore) + (wThroughput * throughputScore);
                scoreNormalized = Clamp(scoreNormalized, 0.0, 1.0);
            }

            // Map to actions per spec
            DecisionAction action;
            if (scoreNormalized >= 0.80) action = DecisionAction.None;
            else if (scoreNormalized >= 0.60) action = DecisionAction.NotifyUser;
            else if (scoreNormalized >= 0.35) action = DecisionAction.Throttle;
            else action = DecisionAction.SwitchAdapter;

            string reason = $"Score={Math.Round(scoreNormalized * 100.0, 2)}% (normalized={scoreNormalized:0.00})";

            var result = new DecisionResult(action, reason);
            return Task.FromResult(result);
        }

        private static double Clamp(double v, double min, double max) => Math.Max(min, Math.Min(max, v));
    }
}
