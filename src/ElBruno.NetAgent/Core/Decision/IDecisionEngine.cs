using System.Threading;
using System.Threading.Tasks;

namespace ElBruno.NetAgent.Core.Decision
{
    public interface IDecisionEngine
    {
        Task<DecisionResult> EvaluateAsync(ElBruno.NetAgent.Core.Models.NetworkQualityReport report, ElBruno.NetAgent.Core.Configuration.NetAgentOptions options, CancellationToken cancellationToken = default);
    }
}
