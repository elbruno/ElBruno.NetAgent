using Xunit;

namespace ElBruno.NetAgent.Tests
{
    public class DecisionEngineTests
    {
        // Simple in-test decision model used as scaffolding for Phase 5 (Decision Engine).
        // TODO: Replace SimpleDecisionEngine with production INetworkDecisionEngine implementation
        private class Decision
        {
            public string Action { get; set; } = string.Empty;
            public string Reason { get; set; } = string.Empty;
        }

        private class SimpleDecisionEngine
        {
            public Decision Decide(double currentScore, double candidateScore, bool inCooldown, int sampleCount, int minSamples = 3, double deltaThresh = 5.0)
            {
                if (sampleCount < minSamples) return new Decision { Action = "Wait", Reason = "Insufficient samples" };
                if (inCooldown) return new Decision { Action = "Stay", Reason = "Cooldown" };
                if (currentScore >= 80.0) return new Decision { Action = "Stay", Reason = "Current healthy" };
                if (candidateScore - currentScore < deltaThresh) return new Decision { Action = "Stay", Reason = "Delta too small" };
                return new Decision { Action = "Switch", Reason = "Candidate better" };
            }
        }

        [Fact]
        public void NoSwitch_When_Current_Healthy()
        {
            var engine = new SimpleDecisionEngine();
            var decision = engine.Decide(currentScore: 85.0, candidateScore: 90.0, inCooldown: false, sampleCount: 5);

            Assert.Equal("Stay", decision.Action);
            Assert.Equal("Current healthy", decision.Reason);
        }

        [Fact]
        public void Switch_When_Current_Poor_And_Candidate_Good()
        {
            var engine = new SimpleDecisionEngine();
            var decision = engine.Decide(currentScore: 40.0, candidateScore: 75.0, inCooldown: false, sampleCount: 5);

            Assert.Equal("Switch", decision.Action);
            Assert.Equal("Candidate better", decision.Reason);
        }

        // TODO: add tests for cooldown, insufficient samples, and small delta cases
    }
}
