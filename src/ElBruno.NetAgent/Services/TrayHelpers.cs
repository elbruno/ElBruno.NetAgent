using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ElBruno.NetAgent.Core.Models;
using ElBruno.NetAgent.Core.Services;
using ElBruno.NetAgent.Core.Decision;

namespace ElBruno.NetAgent.Services
{
    // Lightweight null helpers used by the TrayIconService back-compat constructor and tests.
    internal class NullInventoryService : INetworkInventoryService
    {
        public Task<IReadOnlyList<NetworkInterfaceInfo>> GetInterfacesAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<NetworkInterfaceInfo> empty = Array.Empty<NetworkInterfaceInfo>();
            return Task.FromResult(empty);
        }
    }

    internal class NullQualityMonitor : INetworkQualityMonitor
    {
        public Task<NetworkQualityReport> EvaluateAsync(NetworkInterfaceInfo adapter, CancellationToken cancellationToken)
        {
            var r = new NetworkQualityReport { InterfaceId = adapter?.Id ?? string.Empty, LatencyMs = 0, PacketLossPercent = 0.0, Score = 100.0 };
            return Task.FromResult(r);
        }
    }

    internal class NullDecisionEngine : IDecisionEngine
    {
        public Task<DecisionResult> EvaluateAsync(Core.Models.NetworkQualityReport report, Core.Configuration.NetAgentOptions options, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DecisionResult(Core.Decision.DecisionAction.None, "No-op decision (null engine)"));
        }
    }
}