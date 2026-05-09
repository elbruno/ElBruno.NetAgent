using ElBruno.NetAgent.Core.Enums;
using ElBruno.NetAgent.Core.Models;
using Microsoft.Extensions.Logging;

namespace ElBruno.NetAgent.Services.Decision;

/// <summary>
/// Pure-logic implementation of INetworkDecisionEngine.
/// Evaluates network switch decisions based on quality snapshots and anti-flapping rules.
/// </summary>
public class NetworkDecisionEngine : INetworkDecisionEngine
{
    private readonly ILogger<NetworkDecisionEngine> _logger;

    public NetworkDecisionEngine(ILogger<NetworkDecisionEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Evaluates the decision context using the following rule order:
    /// 1. Auto mode disabled → stay
    /// 2. Switch in progress → stay
    /// 3. Cooldown not elapsed → stay
    /// 4. Insufficient samples → wait
    /// 5. Current healthy → stay
    /// 6. Target incomplete data → stay
    /// 7. Score delta too small → stay
    /// 8. Current unhealthy + better candidate → switch
    /// </summary>
    public NetworkDecision Evaluate(NetworkDecisionContext context)
    {
        // Rule 0: Auto mode must be enabled
        if (!context.AutoModeEnabled)
        {
            return CreateDecision(false, NetworkDecisionReason.None,
                "Auto mode is disabled");
        }

        // Rule 1: Switch in progress
        if (context.IsSwitchInProgress)
        {
            _logger.LogDebug("Decision: switch already in progress, staying");
            return CreateDecision(false, NetworkDecisionReason.StaySwitchInProgress,
                "A switch is already in progress");
        }

        // Rule 2: Cooldown check
        if (context.LastSwitchTime.HasValue)
        {
            var elapsed = (DateTime.UtcNow - context.LastSwitchTime.Value).TotalSeconds;
            if (elapsed < context.FailbackCooldownSeconds)
            {
                _logger.LogDebug("Decision: cooldown not elapsed ({Elapsed:F0}s / {Cooldown}s), staying",
                    elapsed, context.FailbackCooldownSeconds);
                return CreateDecision(false, NetworkDecisionReason.StayCooldown,
                    $"Cooldown not elapsed: {elapsed:F0}s / {context.FailbackCooldownSeconds}s");
            }
        }

        // Rule 3: Insufficient samples for current interface
        if (context.CurrentHistory.Count < context.MinimumChecksBeforeSwitch)
        {
            _logger.LogDebug("Decision: insufficient current samples ({Count} < {Min}), waiting",
                context.CurrentHistory.Count, context.MinimumChecksBeforeSwitch);
            return CreateDecision(false, NetworkDecisionReason.WaitInsufficientSamples,
                $"Insufficient current samples: {context.CurrentHistory.Count} < {context.MinimumChecksBeforeSwitch}");
        }

        // Rule 4: Current interface is healthy
        if (context.CurrentQuality != null && IsHealthy(context.CurrentQuality))
        {
            _logger.LogDebug("Decision: current network is healthy (score {Score}), staying",
                context.CurrentQuality.QualityScore);
            return CreateDecision(false, NetworkDecisionReason.StayCurrentHealthy,
                $"Current network is healthy (score {context.CurrentQuality.QualityScore:F0})");
        }

        // Rule 5: Target interface incomplete data
        if (context.TargetHistory.Count < context.MinimumChecksBeforeSwitch)
        {
            _logger.LogDebug("Decision: insufficient target samples ({Count} < {Min}), staying",
                context.TargetHistory.Count, context.MinimumChecksBeforeSwitch);
            return CreateDecision(false, NetworkDecisionReason.StayIncompleteTargetData,
                $"Insufficient target samples: {context.TargetHistory.Count} < {context.MinimumChecksBeforeSwitch}");
        }

        // Rule 6: Score delta too small
        if (context.CurrentQuality != null && context.TargetQuality != null)
        {
            var delta = context.TargetQuality.QualityScore - context.CurrentQuality.QualityScore;
            if (delta < context.MinimumScoreDeltaToSwitch)
            {
                _logger.LogDebug("Decision: score delta too small ({Delta:F0} < {Min}), staying",
                    delta, context.MinimumScoreDeltaToSwitch);
                return CreateDecision(false, NetworkDecisionReason.StayDeltaTooSmall,
                    $"Score delta too small: {delta:F0} < {context.MinimumScoreDeltaToSwitch}");
            }
        }

        // Rule 7: Current unhealthy + better candidate → switch
        if (context.CurrentQuality != null && context.TargetQuality != null)
        {
            var delta = context.TargetQuality.QualityScore - context.CurrentQuality.QualityScore;
            _logger.LogInformation("Decision: switching to {TargetName} (current={Current:F0}, target={Target:F0}, delta={Delta:F0})",
                context.TargetInterface.Name,
                context.CurrentQuality.QualityScore,
                context.TargetQuality.QualityScore,
                delta);
            return CreateDecision(true, NetworkDecisionReason.SwitchBetterCandidate,
                $"Switching to {context.TargetInterface.Name} (score {context.TargetQuality.QualityScore:F0} vs {context.CurrentQuality.QualityScore:F0}, delta {delta:F0})");
        }

        // Fallback: stay
        _logger.LogDebug("Decision: no clear candidate, staying");
        return CreateDecision(false, NetworkDecisionReason.StayHealthyCurrent,
            "No clear better candidate found");
    }

    private static bool IsHealthy(NetworkQualitySnapshot snapshot) =>
        snapshot.QualityLevel is NetworkQualityLevel.Excellent or NetworkQualityLevel.Good;

    private static NetworkDecision CreateDecision(
        bool shouldSwitch,
        NetworkDecisionReason reason,
        string reasonText)
    {
        return new NetworkDecision
        {
            ShouldSwitch = shouldSwitch,
            Reason = reason,
            ReasonText = reasonText
        };
    }
}
