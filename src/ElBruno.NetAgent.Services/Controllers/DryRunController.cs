using System.Threading;
using System.Threading.Tasks;
using ElBruno.NetAgent.Core.Controllers;
using ElBruno.NetAgent.Core.Decision;

namespace ElBruno.NetAgent.Services.Controllers
{
    public class DryRunController : IDryRunController
    {
        public Task<ExecutionResult> ExecuteAsync(DecisionResult decision, bool dryRun, CancellationToken cancellationToken = default)
        {
            if (dryRun)
            {
                return Task.FromResult(new ExecutionResult(true, "Dry-run: no action executed"));
            }
            else
            {
                return Task.FromResult(new ExecutionResult(true, "Dry-run disabled: real execution not implemented in this abstraction"));
            }
        }
    }
}
