using System.Threading;
using System.Threading.Tasks;

namespace ElBruno.NetAgent.Core.Services
{
    /// <summary>
    /// Abstraction for testing network endpoints. Implementations may ping or otherwise probe endpoints.
    /// Tests should inject a fake implementation to avoid real network IO.
    /// </summary>
    public interface INetworkQualityTester
    {
        /// <summary>
        /// Tests the provided endpoints and returns an aggregated latency (ms) and packet loss (percent).
        /// </summary>
        Task<(int LatencyMs, double PacketLossPercent)> TestEndpointsAsync(string[] endpoints, CancellationToken cancellationToken);
    }
}
