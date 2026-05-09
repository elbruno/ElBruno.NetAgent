using ElBruno.NetAgent.Core.Enums;
using ElBruno.NetAgent.Core.Models;
using ElBruno.NetAgent.Services.Decision;
using Microsoft.Extensions.Logging;

namespace ElBruno.NetAgent.Tests;

public class NetworkDecisionEngineTests
{
    private readonly ILogger<NetworkDecisionEngine> _logger;

    public NetworkDecisionEngineTests()
    {
        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<NetworkDecisionEngine>();
    }

    private NetworkDecisionEngine CreateEngine() => new(_logger);

    private NetworkDecisionContext CreateContext(
        bool autoModeEnabled = true,
        NetworkQualityLevel? currentQuality = null,
        NetworkQualityLevel? targetQuality = null,
        int currentHistoryCount = 3,
        int targetHistoryCount = 3,
        int minimumChecksBeforeSwitch = 3,
        int minimumScoreDeltaToSwitch = 20,
        int failbackCooldownSeconds = 120,
        DateTime? lastSwitchTime = null,
        bool isSwitchInProgress = false)
    {
        var currentQualityLevel = currentQuality ?? NetworkQualityLevel.Excellent;
        var targetQualityLevel = targetQuality ?? NetworkQualityLevel.Good;

        return new NetworkDecisionContext
        {
            AutoModeEnabled = autoModeEnabled,
            CurrentInterface = new NetworkInterfaceInfo { Id = "current-1", Name = "Current Ethernet" },
            TargetInterface = new NetworkInterfaceInfo { Id = "target-1", Name = "Target WiFi" },
            CurrentQuality = new NetworkQualitySnapshot
            {
                InterfaceId = "current-1",
                InterfaceName = "Current Ethernet",
                QualityScore = MapToScore(currentQualityLevel),
                QualityLevel = currentQualityLevel,
                Timestamp = DateTime.UtcNow,
                PacketLossPercent = MapToPacketLoss(currentQualityLevel),
                AverageLatencyMs = MapToLatency(currentQualityLevel),
                EndpointResults = Array.Empty<EndpointPingResult>()
            },
            TargetQuality = new NetworkQualitySnapshot
            {
                InterfaceId = "target-1",
                InterfaceName = "Target WiFi",
                QualityScore = MapToScore(targetQualityLevel),
                QualityLevel = targetQualityLevel,
                Timestamp = DateTime.UtcNow,
                PacketLossPercent = MapToPacketLoss(targetQualityLevel),
                AverageLatencyMs = MapToLatency(targetQualityLevel),
                EndpointResults = Array.Empty<EndpointPingResult>()
            },
            CurrentHistory = GenerateSnapshots(currentHistoryCount, currentQualityLevel),
            TargetHistory = GenerateSnapshots(targetHistoryCount, targetQualityLevel),
            MinimumChecksBeforeSwitch = minimumChecksBeforeSwitch,
            MinimumScoreDeltaToSwitch = minimumScoreDeltaToSwitch,
            FailbackCooldownSeconds = failbackCooldownSeconds,
            LastSwitchTime = lastSwitchTime,
            IsSwitchInProgress = isSwitchInProgress
        };
    }

    private static double MapToScore(NetworkQualityLevel level) => level switch
    {
        NetworkQualityLevel.Excellent => 95,
        NetworkQualityLevel.Good => 75,
        NetworkQualityLevel.Fair => 50,
        NetworkQualityLevel.Poor => 25,
        _ => 10
    };

    private static double MapToPacketLoss(NetworkQualityLevel level) => level switch
    {
        NetworkQualityLevel.Excellent => 0,
        NetworkQualityLevel.Good => 1,
        NetworkQualityLevel.Fair => 5,
        NetworkQualityLevel.Poor => 15,
        _ => 50
    };

    private static double MapToLatency(NetworkQualityLevel level) => level switch
    {
        NetworkQualityLevel.Excellent => 10,
        NetworkQualityLevel.Good => 50,
        NetworkQualityLevel.Fair => 120,
        NetworkQualityLevel.Poor => 250,
        _ => 500
    };

    private static IReadOnlyList<NetworkQualitySnapshot> GenerateSnapshots(int count, NetworkQualityLevel level)
    {
        var snapshots = new List<NetworkQualitySnapshot>();
        for (var i = 0; i < count; i++)
        {
            snapshots.Add(new NetworkQualitySnapshot
            {
                InterfaceId = "current-1",
                InterfaceName = "Current Ethernet",
                QualityScore = MapToScore(level),
                QualityLevel = level,
                Timestamp = DateTime.UtcNow.AddSeconds(-i * 10),
                PacketLossPercent = MapToPacketLoss(level),
                AverageLatencyMs = MapToLatency(level),
                EndpointResults = Array.Empty<EndpointPingResult>()
            });
        }
        return snapshots;
    }

    #region Rule: Auto mode disabled

    [Fact]
    public void Evaluate_AutoModeDisabled_ReturnsStayWithNoneReason()
    {
        // Arrange
        var engine = CreateEngine();
        var context = CreateContext(autoModeEnabled: false);

        // Act
        var decision = engine.Evaluate(context);

        // Assert
        Assert.False(decision.ShouldSwitch);
        Assert.Equal(NetworkDecisionReason.None, decision.Reason);
        Assert.Contains("Auto mode is disabled", decision.ReasonText);
    }

    #endregion

    #region Rule: Switch in progress

    [Fact]
    public void Evaluate_SwitchInProgress_ReturnsStayWithSwitchInProgressReason()
    {
        // Arrange
        var engine = CreateEngine();
        var context = CreateContext(
            autoModeEnabled: true,
            currentQuality: NetworkQualityLevel.Poor,
            targetQuality: NetworkQualityLevel.Excellent,
            isSwitchInProgress: true);

        // Act
        var decision = engine.Evaluate(context);

        // Assert
        Assert.False(decision.ShouldSwitch);
        Assert.Equal(NetworkDecisionReason.StaySwitchInProgress, decision.Reason);
        Assert.StartsWith("A switch", decision.ReasonText, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Rule: Cooldown active

    [Fact]
    public void Evaluate_CooldownNotElapsed_ReturnsStayWithCooldownReason()
    {
        // Arrange
        var engine = CreateEngine();
        var context = CreateContext(
            autoModeEnabled: true,
            currentQuality: NetworkQualityLevel.Poor,
            targetQuality: NetworkQualityLevel.Excellent,
            failbackCooldownSeconds: 120,
            lastSwitchTime: DateTime.UtcNow.AddSeconds(-60));

        // Act
        var decision = engine.Evaluate(context);

        // Assert
        Assert.False(decision.ShouldSwitch);
        Assert.Equal(NetworkDecisionReason.StayCooldown, decision.Reason);
        Assert.Contains("Cooldown", decision.ReasonText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_CooldownElapsed_AllowsFurtherEvaluation()
    {
        // Arrange
        var engine = CreateEngine();
        // Current is unhealthy, target is excellent, cooldown elapsed (300s ago)
        var context = CreateContext(
            autoModeEnabled: true,
            currentQuality: NetworkQualityLevel.Poor,
            targetQuality: NetworkQualityLevel.Excellent,
            failbackCooldownSeconds: 120,
            lastSwitchTime: DateTime.UtcNow.AddSeconds(-300));

        // Act
        var decision = engine.Evaluate(context);

        // Assert - should proceed past cooldown check
        // Since current is unhealthy and target is excellent with large delta, it should recommend switch
        Assert.True(decision.ShouldSwitch);
        Assert.Equal(NetworkDecisionReason.SwitchBetterCandidate, decision.Reason);
    }

    #endregion

    #region Rule: Insufficient samples (current)

    [Fact]
    public void Evaluate_InsufficientCurrentSamples_ReturnsWaitWithInsufficientSamplesReason()
    {
        // Arrange
        var engine = CreateEngine();
        var context = CreateContext(
            autoModeEnabled: true,
            currentQuality: NetworkQualityLevel.Poor,
            targetQuality: NetworkQualityLevel.Excellent,
            currentHistoryCount: 2,
            minimumChecksBeforeSwitch: 3);

        // Act
        var decision = engine.Evaluate(context);

        // Assert
        Assert.False(decision.ShouldSwitch);
        Assert.Equal(NetworkDecisionReason.WaitInsufficientSamples, decision.Reason);
        Assert.Contains("Insufficient current samples", decision.ReasonText, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Rule: Current healthy → stay

    [Fact]
    public void Evaluate_CurrentHealthy_ReturnsStayWithHealthyCurrentReason()
    {
        // Arrange
        var engine = CreateEngine();
        var context = CreateContext(
            autoModeEnabled: true,
            currentQuality: NetworkQualityLevel.Excellent,
            targetQuality: NetworkQualityLevel.Good);

        // Act
        var decision = engine.Evaluate(context);

        // Assert
        Assert.False(decision.ShouldSwitch);
        Assert.Equal(NetworkDecisionReason.StayCurrentHealthy, decision.Reason);
        Assert.Contains("healthy", decision.ReasonText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_CurrentGood_ReturnsStayWithHealthyCurrentReason()
    {
        // Arrange
        var engine = CreateEngine();
        var context = CreateContext(
            autoModeEnabled: true,
            currentQuality: NetworkQualityLevel.Good,
            targetQuality: NetworkQualityLevel.Fair);

        // Act
        var decision = engine.Evaluate(context);

        // Assert
        Assert.False(decision.ShouldSwitch);
        Assert.Equal(NetworkDecisionReason.StayCurrentHealthy, decision.Reason);
    }

    #endregion

    #region Rule: Insufficient target samples

    [Fact]
    public void Evaluate_InsufficientTargetSamples_ReturnsStayWithIncompleteTargetDataReason()
    {
        // Arrange
        var engine = CreateEngine();
        var context = CreateContext(
            autoModeEnabled: true,
            currentQuality: NetworkQualityLevel.Poor,
            targetQuality: NetworkQualityLevel.Excellent,
            currentHistoryCount: 5,
            targetHistoryCount: 1,
            minimumChecksBeforeSwitch: 3);

        // Act
        var decision = engine.Evaluate(context);

        // Assert
        Assert.False(decision.ShouldSwitch);
        Assert.Equal(NetworkDecisionReason.StayIncompleteTargetData, decision.Reason);
        Assert.Contains("Insufficient target samples", decision.ReasonText, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Rule: Score delta too small

    [Fact]
    public void Evaluate_ScoreDeltaTooSmall_ReturnsStayWithDeltaTooSmallReason()
    {
        // Arrange
        var engine = CreateEngine();
        var context = CreateContext(
            autoModeEnabled: true,
            currentQuality: NetworkQualityLevel.Poor,
            targetQuality: NetworkQualityLevel.Fair,
            currentHistoryCount: 5,
            targetHistoryCount: 5,
            minimumScoreDeltaToSwitch: 30);

        // Act
        var decision = engine.Evaluate(context);

        // Assert
        Assert.False(decision.ShouldSwitch);
        Assert.Equal(NetworkDecisionReason.StayDeltaTooSmall, decision.Reason);
        Assert.Contains("delta", decision.ReasonText, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Rule: Current unhealthy + better candidate → switch

    [Fact]
    public void Evaluate_CurrentUnhealthy_BetterTarget_ReturnsSwitch()
    {
        // Arrange
        var engine = CreateEngine();
        var context = CreateContext(
            autoModeEnabled: true,
            currentQuality: NetworkQualityLevel.Poor,
            targetQuality: NetworkQualityLevel.Excellent,
            currentHistoryCount: 5,
            targetHistoryCount: 5);

        // Act
        var decision = engine.Evaluate(context);

        // Assert
        Assert.True(decision.ShouldSwitch);
        Assert.Equal(NetworkDecisionReason.SwitchBetterCandidate, decision.Reason);
        Assert.Contains("Switching", decision.ReasonText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_CurrentFair_BetterTarget_ReturnsSwitch()
    {
        // Arrange
        var engine = CreateEngine();
        var context = CreateContext(
            autoModeEnabled: true,
            currentQuality: NetworkQualityLevel.Fair,
            targetQuality: NetworkQualityLevel.Good,
            currentHistoryCount: 5,
            targetHistoryCount: 5,
            minimumScoreDeltaToSwitch: 15);

        // Act
        var decision = engine.Evaluate(context);

        // Assert
        Assert.True(decision.ShouldSwitch);
        Assert.Equal(NetworkDecisionReason.SwitchBetterCandidate, decision.Reason);
    }

    #endregion

    #region Edge cases

    [Fact]
    public void Evaluate_DryRunModeDoesNotAffectDecision_ReturnsDecision()
    {
        // Arrange
        var engine = CreateEngine();
        var context = CreateContext(
            autoModeEnabled: true,
            currentQuality: NetworkQualityLevel.Poor,
            targetQuality: NetworkQualityLevel.Excellent,
            currentHistoryCount: 5,
            targetHistoryCount: 5);
        // DryRunMode is true by default in context creation, but it doesn't affect logic

        // Act
        var decision = engine.Evaluate(context);

        // Assert
        Assert.True(decision.ShouldSwitch);
        Assert.Equal(NetworkDecisionReason.SwitchBetterCandidate, decision.Reason);
    }

    [Fact]
    public void Evaluate_NoLastSwitchTime_PassesCooldownCheck()
    {
        // Arrange
        var engine = CreateEngine();
        var context = CreateContext(
            autoModeEnabled: true,
            currentQuality: NetworkQualityLevel.Poor,
            targetQuality: NetworkQualityLevel.Excellent,
            lastSwitchTime: null);

        // Act
        var decision = engine.Evaluate(context);

        // Assert
        Assert.True(decision.ShouldSwitch);
        Assert.Equal(NetworkDecisionReason.SwitchBetterCandidate, decision.Reason);
    }

    [Fact]
    public void Evaluate_TargetNotBetter_ReturnsStay()
    {
        // Arrange
        var engine = CreateEngine();
        var context = CreateContext(
            autoModeEnabled: true,
            currentQuality: NetworkQualityLevel.Poor,
            targetQuality: NetworkQualityLevel.Poor,
            currentHistoryCount: 5,
            targetHistoryCount: 5);

        // Act
        var decision = engine.Evaluate(context);

        // Assert - both have same score (Poor=25), delta=0 < 20, so delta rule triggers first
        Assert.False(decision.ShouldSwitch);
        Assert.Equal(NetworkDecisionReason.StayDeltaTooSmall, decision.Reason);
    }

    [Fact]
    public void Evaluate_TargetMuchWorse_ReturnsStay()
    {
        // Arrange
        var engine = CreateEngine();
        var context = CreateContext(
            autoModeEnabled: true,
            currentQuality: NetworkQualityLevel.Good,
            targetQuality: NetworkQualityLevel.Poor,
            currentHistoryCount: 5,
            targetHistoryCount: 5);

        // Act
        var decision = engine.Evaluate(context);

        // Assert
        Assert.False(decision.ShouldSwitch);
        Assert.Equal(NetworkDecisionReason.StayCurrentHealthy, decision.Reason);
    }

    #endregion
}
