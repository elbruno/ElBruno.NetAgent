using System.Threading;
using System.Threading.Tasks;
using ElBruno.NetAgent.Core.Decision;
using ElBruno.NetAgent.Core.Models;
using ElBruno.NetAgent.Core.Configuration;

namespace ElBruno.NetAgent.Tests
{
    public class TestDecisionEngine : IDecisionEngine
    {
        public Task<DecisionResult> EvaluateAsync(NetworkQualityReport report, NetAgentOptions options, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DecisionResult(Core.Decision.DecisionAction.None, "Test no-op"));
        }
    }
}
