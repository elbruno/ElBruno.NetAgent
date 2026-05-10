using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ElBruno.NetAgent.Tests
{
    public class SwitchingRulesTests
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

        [Fact]
        public async Task EvaluateAsync_Excludes_By_Kind()
        {
            var engineType = FindTypeByName("InMemoryDecisionEngine");
            Assert.NotNull(engineType);
            var engineObj = Activator.CreateInstance(engineType!, nonPublic: true);
            var engine = (ElBruno.NetAgent.Core.Decision.IDecisionEngine)engineObj!;

            var report = new ElBruno.NetAgent.Core.Models.NetworkQualityReport { InterfaceId = "id1", InterfaceName = "lo0", InterfaceKind = "Loopback", LatencyMs = 10, PacketLossPercent = 0, Score = 100 };
            var options = new ElBruno.NetAgent.Core.Configuration.NetAgentOptions();
            options.SwitchingRules = new ElBruno.NetAgent.Core.Configuration.NetAgentOptions.SwitchingRulesOptions { ExcludedInterfaceKinds = new string[] { "Loopback" } };

            var result = await engine.EvaluateAsync(report, options, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(ElBruno.NetAgent.Core.Decision.DecisionAction.None, result.Action);
            Assert.Contains("Excluded", result.Reason, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task EvaluateAsync_Excludes_By_Pattern()
        {
            var engineType = FindTypeByName("InMemoryDecisionEngine");
            Assert.NotNull(engineType);
            var engineObj = Activator.CreateInstance(engineType!, nonPublic: true);
            var engine = (ElBruno.NetAgent.Core.Decision.IDecisionEngine)engineObj!;

            var report = new ElBruno.NetAgent.Core.Models.NetworkQualityReport { InterfaceId = "vpn-123", InterfaceName = "MyVPNAdapter", InterfaceKind = "Ethernet", LatencyMs = 500, PacketLossPercent = 1.0, Score = 20 };
            var options = new ElBruno.NetAgent.Core.Configuration.NetAgentOptions();
            options.SwitchingRules = new ElBruno.NetAgent.Core.Configuration.NetAgentOptions.SwitchingRulesOptions { ExcludedInterfacePatterns = new string[] { "vpn" } };

            var result = await engine.EvaluateAsync(report, options, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(ElBruno.NetAgent.Core.Decision.DecisionAction.None, result.Action);
            Assert.Contains("Excluded", result.Reason, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task EvaluateAsync_PreferPattern_Boosts_Score()
        {
            var engineType = FindTypeByName("InMemoryDecisionEngine");
            Assert.NotNull(engineType);
            var engineObj = Activator.CreateInstance(engineType!, nonPublic: true);
            var engine = (ElBruno.NetAgent.Core.Decision.IDecisionEngine)engineObj!;

            // Base report would be poor -> SwitchAdapter. With preferred pattern should be boosted into Throttle
            var report = new ElBruno.NetAgent.Core.Models.NetworkQualityReport { InterfaceId = "cand1", InterfaceName = "PreferredNet", InterfaceKind = "Ethernet", LatencyMs = 700, PacketLossPercent = 20.0, Score = 0 };
            var options = new ElBruno.NetAgent.Core.Configuration.NetAgentOptions();
            options.SwitchingRules = new ElBruno.NetAgent.Core.Configuration.NetAgentOptions.SwitchingRulesOptions { PreferredInterfacePatterns = new string[] { "Preferred" } };

            var result = await engine.EvaluateAsync(report, options, CancellationToken.None).ConfigureAwait(false);
            Assert.NotEqual(ElBruno.NetAgent.Core.Decision.DecisionAction.SwitchAdapter, result.Action);
            Assert.Contains("preferred", result.Reason, StringComparison.OrdinalIgnoreCase);
        }
    }
}
