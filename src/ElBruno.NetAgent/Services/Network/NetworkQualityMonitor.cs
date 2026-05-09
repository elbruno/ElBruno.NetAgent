using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ElBruno.NetAgent.Core.Models;
using ElBruno.NetAgent.Core.Services;
using ElBruno.NetAgent.Core.Configuration;

namespace ElBruno.NetAgent.Services.Network
{
    /// <summary>
    /// Phase 4: Network quality monitor. Read-only evaluation of network adapters.
    /// Does not alter system network settings. Uses an INetworkQualityTester for probes
    /// so tests can inject a fake implementation.
    /// </summary>
    public class NetworkQualityMonitor : ElBruno.NetAgent.Core.Services.INetworkQualityMonitor
    {
        private readonly INetworkQualityTester _tester;
        private readonly NetAgentOptions _options;
        private readonly ILogger<NetworkQualityMonitor> _logger;

        public NetworkQualityMonitor(INetworkQualityTester tester, NetAgentOptions options, ILogger<NetworkQualityMonitor> logger)
        {
            _tester = tester ?? throw new ArgumentNullException(nameof(tester));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<NetworkQualityReport> EvaluateAsync(NetworkInterfaceInfo adapter, CancellationToken cancellationToken)
        {
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));

            // Probe configured endpoints using the injected tester. Tests supply a fake tester to avoid real network IO.
            var endpoints = _options.TestEndpoints ?? Array.Empty<string>();
            var (latency, loss) = await _tester.TestEndpointsAsync(endpoints, cancellationToken).ConfigureAwait(false);

            // Simple scoring formula (higher is better):
            // Start at 100, penalize for latency relative to threshold and for packet loss.
            double score = 100.0;
            var latencyFactor = (double)latency / Math.Max(1, _options.LatencyThresholdMs);
            score -= Math.Min(50.0, latencyFactor * 50.0);

            // Penalize packet loss (each percent reduces score by up to 2 points, capped)
            score -= Math.Min(80.0, loss * 2.0);

            score = Math.Max(0.0, Math.Min(100.0, score));

            var report = new NetworkQualityReport
            {
                InterfaceId = adapter.Id,
                LatencyMs = latency,
                PacketLossPercent = loss,
                Score = Math.Round(score, 2)
            };

            _logger.LogInformation("NetworkQualityMonitor: {Name} Id:{Id} latency={Latency}ms loss={Loss}% score={Score}", adapter.Name, adapter.Id, report.LatencyMs, report.PacketLossPercent, report.Score);

            return report;
        }
    }
}
