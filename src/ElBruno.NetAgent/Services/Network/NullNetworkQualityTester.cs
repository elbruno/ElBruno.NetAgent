using System.Threading;
using System.Threading.Tasks;
using ElBruno.NetAgent.Core.Services;

namespace ElBruno.NetAgent.Services.Network
{
    /// <summary>
    /// Null implementation used at runtime by default to avoid performing real network probes.
    /// Returns neutral, non-failing values suitable for dry-run UI previews.
    /// </summary>
    public class NullNetworkQualityTester : INetworkQualityTester
    {
        public Task<(int LatencyMs, double PacketLossPercent)> TestEndpointsAsync(string[] endpoints, CancellationToken cancellationToken)
        {
            // Neutral defaults: modest latency, no packet loss.
            return Task.FromResult((LatencyMs: 50, PacketLossPercent: 0.0));
        }
    }
}