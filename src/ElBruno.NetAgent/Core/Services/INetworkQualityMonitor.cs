using System.Threading;
using System.Threading.Tasks;
using ElBruno.NetAgent.Core.Models;

namespace ElBruno.NetAgent.Core.Services
{
    /// <summary>
    /// Evaluates network quality for a single adapter and returns a report.
    /// Phase 4: Network quality monitor (read-only; does not mutate system configuration).
    /// </summary>
    public interface INetworkQualityMonitor
    {
        Task<NetworkQualityReport> EvaluateAsync(NetworkInterfaceInfo adapter, CancellationToken cancellationToken);
    }
}
