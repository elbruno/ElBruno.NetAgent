using System.Threading.Tasks;
using Xunit;
using ElBruno.NetAgent.Core.Configuration;
using ElBruno.NetAgent.Tests.Helpers;
using Microsoft.Extensions.Logging;

namespace ElBruno.NetAgent.Tests
{
    public class AutoModeHostedServiceTests
    {
        // Minimal decision model used for tests
        private class Decision
        {
            public string Action { get; set; } = "Stay";
            public string CandidateInterface { get; set; } = string.Empty;
            public string Reason { get; set; } = string.Empty;
        }

        // Test interfaces (faked within tests to avoid coupling to not-yet-implemented production types)
        private interface INetworkQualityService
        {
            Task<(double CurrentScore, double CandidateScore, string CandidateInterface)> GetLatestAsync();
        }

        private interface INetworkDecisionEngine
        {
            Decision Decide(double currentScore, double candidateScore);
        }

        private interface INetworkController
        {
            string AttemptPrefer(string interfaceId);
        }

        // Testable AutoModeHostedService - one-shot evaluation (no background loop) for fast, deterministic tests
        private class AutoModeHostedService
        {
            private readonly INetworkQualityService _quality;
            private readonly INetworkDecisionEngine _engine;
            private readonly INetworkController _controller;
            private readonly NetAgentOptions _options;
            private readonly ILogger<AutoModeHostedService> _logger;

            public AutoModeHostedService(INetworkQualityService quality, INetworkDecisionEngine engine, INetworkController controller, NetAgentOptions options, ILogger<AutoModeHostedService> logger)
            {
                _quality = quality;
                _engine = engine;
                _controller = controller;
                _options = options;
                _logger = logger;
            }

            public async Task StartAsync()
            {
                if (!_options.AutoModeEnabled)
                {
                    _logger.LogInformation("Auto mode disabled");
                    return;
                }

                var snap = await _quality.GetLatestAsync();
                var decision = _engine.Decide(snap.CurrentScore, snap.CandidateScore);

                if (decision.Action == "Switch")
                {
                    if (_options.DryRunMode)
                    {
                        _logger.LogInformation($"DryRun: would prefer {decision.CandidateInterface}");
                    }
                    else
                    {
                        var res = _controller.AttemptPrefer(decision.CandidateInterface);
                        _logger.LogInformation($"Controller result: {res}");
                    }
                }
                else
                {
                    _logger.LogInformation($"Decision: {decision.Action} - {decision.Reason}");
                }
            }
        }

        // Fakes
        private class FakeQualityService : INetworkQualityService
        {
            private readonly double _current;
            private readonly double _candidate;
            private readonly string _candidateInterface;
            public bool WasCalled { get; private set; }

            public FakeQualityService(double current, double candidate, string candidateInterface = "if1")
            {
                _current = current;
                _candidate = candidate;
                _candidateInterface = candidateInterface;
            }

            public Task<(double CurrentScore, double CandidateScore, string CandidateInterface)> GetLatestAsync()
            {
                WasCalled = true;
                return Task.FromResult((_current, _candidate, _candidateInterface));
            }
        }

        private class FakeDecisionEngine : INetworkDecisionEngine
        {
            private readonly Decision _decision;
            public FakeDecisionEngine(Decision decision) => _decision = decision;
            public Decision Decide(double currentScore, double candidateScore) => _decision;
        }

        private class FakeController : INetworkController
        {
            public bool WasCalled { get; private set; }
            public string? CalledWith { get; private set; }

            public string AttemptPrefer(string interfaceId)
            {
                WasCalled = true;
                CalledWith = interfaceId;
                return $"Preferred {interfaceId}";
            }
        }

        [Fact]
        public async Task AutoMode_DoesNotRunWhenDisabled()
        {
            var options = new NetAgentOptions { AutoModeEnabled = false };
            var quality = new FakeQualityService(current: 10, candidate: 90);
            var engine = new FakeDecisionEngine(new Decision { Action = "Switch", CandidateInterface = "if1" });
            var controller = new FakeController();
            var logger = new TestLogger<AutoModeHostedService>();

            var svc = new AutoModeHostedService(quality, engine, controller, options, logger);
            await svc.StartAsync();

            Assert.False(quality.WasCalled, "Quality service should not be queried when AutoMode is disabled");
            Assert.Contains(logger.Entries, e => e.Message.Contains("Auto mode disabled"));
            Assert.False(controller.WasCalled, "Controller must not be invoked when AutoMode is disabled");
        }

        [Fact]
        public async Task AutoMode_LogsSwitchWhenDryRun()
        {
            var options = new NetAgentOptions { AutoModeEnabled = true, DryRunMode = true };
            var quality = new FakeQualityService(current: 20, candidate: 90, candidateInterface: "if1");
            var engine = new FakeDecisionEngine(new Decision { Action = "Switch", CandidateInterface = "if1", Reason = "Candidate better" });
            var controller = new FakeController();
            var logger = new TestLogger<AutoModeHostedService>();

            var svc = new AutoModeHostedService(quality, engine, controller, options, logger);
            await svc.StartAsync();

            Assert.True(quality.WasCalled, "Quality service should be queried when AutoMode is enabled");
            Assert.Contains(logger.Entries, e => e.Message.Contains("DryRun: would prefer if1"));
            Assert.False(controller.WasCalled, "Controller must not be invoked in DryRun mode");
        }

        [Fact]
        public async Task AutoMode_InvokesControllerWhenNotDryRun()
        {
            var options = new NetAgentOptions { AutoModeEnabled = true, DryRunMode = false };
            var quality = new FakeQualityService(current: 20, candidate: 90, candidateInterface: "if1");
            var engine = new FakeDecisionEngine(new Decision { Action = "Switch", CandidateInterface = "if1", Reason = "Candidate better" });
            var controller = new FakeController();
            var logger = new TestLogger<AutoModeHostedService>();

            var svc = new AutoModeHostedService(quality, engine, controller, options, logger);
            await svc.StartAsync();

            Assert.True(quality.WasCalled, "Quality service should be queried when AutoMode is enabled");
            Assert.True(controller.WasCalled, "Controller must be invoked when not in DryRun mode");
            Assert.Equal("if1", controller.CalledWith);
            Assert.Contains(logger.Entries, e => e.Message.Contains("Controller result:"));
        }
    }
}
