using System.Threading;
using System.Threading.Tasks;
using ElBruno.NetAgent.Core.Models;
using ElBruno.NetAgent.Core.Services;

namespace ElBruno.NetAgent.Tests.Helpers
{
    internal class FakeNetworkQualityMonitor : INetworkQualityMonitor
    {
        public bool WasCalled { get; private set; }
        public NetworkQualityReport ReportToReturn { get; set; } = new NetworkQualityReport { LatencyMs = 10, PacketLossPercent = 0, Score = 90 };

        public Task<NetworkQualityReport> EvaluateAsync(NetworkInterfaceInfo adapter, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult(ReportToReturn);
        }
    }
}
