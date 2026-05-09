using System.Threading;
using System.Threading.Tasks;

namespace ElBruno.NetAgent.Core.Controllers
{
    public interface IDryRunController
    {
        Task<ExecutionResult> ExecuteAsync(ElBruno.NetAgent.Core.Decision.DecisionResult decision, bool dryRun, CancellationToken cancellationToken = default);
    }
}
