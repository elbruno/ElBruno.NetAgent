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
        private Type FindTypeByName(string name)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = null;
                try { t = asm.GetTypes().FirstOrDefault(x => x.Name == name); }
                catch (ReflectionTypeLoadException) { continue; }
                if (t != null) return t;
            }
            return null;
        }

        private async Task<string> EvaluateActionNameAsync(object engineInstance, object reportInstance)
        {
            var method = engineInstance.GetType().GetMethod("EvaluateAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var taskObj = (Task)method.Invoke(engineInstance, new[] { reportInstance, null, CancellationToken.None });
            await taskObj.ConfigureAwait(false);
            var resultProp = taskObj.GetType().GetProperty("Result");
            var result = resultProp.GetValue(taskObj);
            var actionProp = result.GetType().GetProperty("Action");
            var actionValue = actionProp.GetValue(result);
            return actionValue?.ToString() ?? string.Empty;
        }

        private object CreateAndPopulateReport(int latencyMs, double packetLossPercent, double score = 0)
        {
            var type = FindTypeByName("NetworkQualityReport");
            if (type == null)
            {
                throw new InvalidOperationException("Production NetworkQualityReport type not found.");
            }
            var inst = Activator.CreateInstance(type);
            var pLatency = type.GetProperty("LatencyMs");
            pLatency.SetValue(inst, latencyMs);
            var pPacket = type.GetProperty("PacketLossPercent");
            pPacket.SetValue(inst, packetLossPercent);
            var pScore = type.GetProperty("Score");
            if (pScore != null) pScore.SetValue(inst, score);
            return inst;
        }

        [Fact]
        public async Task EvaluateAsync_Returns_None_For_Healthy()
        {
            var engineType = FindTypeByName("InMemoryDecisionEngine");
            Assert.NotNull(engineType);
            var engine = Activator.CreateInstance(engineType, nonPublic: true);
            // Pass null report to exercise default healthy path
            var action = await EvaluateActionNameAsync(engine, null);
            Assert.Equal("None", action);
        }

        [Fact]
        public async Task EvaluateAsync_Returns_NotifyUser_For_Mild_Degradation()
        {
            var engineType = FindTypeByName("InMemoryDecisionEngine");
            Assert.NotNull(engineType);
            var engine = Activator.CreateInstance(engineType, nonPublic: true);
            var report = CreateAndPopulateReport(250, 40.0);
            var action = await EvaluateActionNameAsync(engine, report);
            Assert.Equal("NotifyUser", action);
        }

        [Fact]
        public async Task EvaluateAsync_Returns_SwitchAdapter_For_Severe_Degradation()
        {
            var engineType = FindTypeByName("InMemoryDecisionEngine");
            Assert.NotNull(engineType);
            var engine = Activator.CreateInstance(engineType, nonPublic: true);
            var report = CreateAndPopulateReport(950, 99.0);
            var action = await EvaluateActionNameAsync(engine, report);
            Assert.Equal("SwitchAdapter", action);
        }
    }
}
