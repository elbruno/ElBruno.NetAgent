using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ElBruno.NetAgent.Tests
{
    public class DecisionEngineImplTests
    {
        private Type? FindTypeByName(string name)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type? t = null;
                try { t = asm.GetTypes().FirstOrDefault(x => x.Name == name); }
                catch (ReflectionTypeLoadException) { continue; }
                if (t != null) return t;
            }
            return null;
        }

        private async Task<string> EvaluateActionNameAsync(ElBruno.NetAgent.Core.Decision.IDecisionEngine engine, ElBruno.NetAgent.Core.Models.NetworkQualityReport? report)
        {
            ElBruno.NetAgent.Core.Configuration.NetAgentOptions? options = null;
            var result = await engine.EvaluateAsync(report!, options!, CancellationToken.None).ConfigureAwait(false);
            return result.Action.ToString();
        }

        private ElBruno.NetAgent.Core.Models.NetworkQualityReport CreateAndPopulateReport(int latencyMs, double packetLossPercent, double score = 0)
        {
            var report = new ElBruno.NetAgent.Core.Models.NetworkQualityReport
            {
                LatencyMs = latencyMs,
                PacketLossPercent = packetLossPercent,
                Score = score
            };
            return report;
        }

        [Fact]
        public async Task EvaluateAsync_Returns_None_For_Healthy()
        {
            var engineType = FindTypeByName("InMemoryDecisionEngine");
            Assert.NotNull(engineType);
            var engineObj = Activator.CreateInstance(engineType!, nonPublic: true);
            var engine = (ElBruno.NetAgent.Core.Decision.IDecisionEngine)engineObj!;
            // Pass null report to exercise default healthy path
            var action = await EvaluateActionNameAsync(engine, null);
            Assert.Equal("None", action);
        }

        [Fact]
        public async Task EvaluateAsync_Returns_NotifyUser_For_Mild_Degradation()
        {
            var engineType = FindTypeByName("InMemoryDecisionEngine");
            Assert.NotNull(engineType);
            var engineObj = Activator.CreateInstance(engineType!, nonPublic: true);
            var engine = (ElBruno.NetAgent.Core.Decision.IDecisionEngine)engineObj!;
            var report = CreateAndPopulateReport(250, 40.0);
            var action = await EvaluateActionNameAsync(engine, report);
            Assert.Equal("NotifyUser", action);
        }

        [Fact]
        public async Task EvaluateAsync_Returns_SwitchAdapter_For_Severe_Degradation()
        {
            var engineType = FindTypeByName("InMemoryDecisionEngine");
            Assert.NotNull(engineType);
            var engineObj = Activator.CreateInstance(engineType!, nonPublic: true);
            var engine = (ElBruno.NetAgent.Core.Decision.IDecisionEngine)engineObj!;
            var report = CreateAndPopulateReport(950, 99.0);
            var action = await EvaluateActionNameAsync(engine, report);
            Assert.Equal("SwitchAdapter", action);
        }
    }
}
