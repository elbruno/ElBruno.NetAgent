using ElBruno.NetAgent.Core.Models;

namespace ElBruno.NetAgent.Services.Decision;

/// <summary>
/// Service responsible for evaluating network switch decisions.
/// </summary>
public interface INetworkDecisionEngine
{
    /// <summary>
    /// Evaluates the given context and returns a decision.
    /// </summary>
    NetworkDecision Evaluate(NetworkDecisionContext context);
}
