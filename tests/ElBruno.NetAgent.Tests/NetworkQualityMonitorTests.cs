using System.Threading.Tasks;
using System.Threading;
using Xunit;
using ElBruno.NetAgent.Core.Models;
using ElBruno.NetAgent.Core.Services;
using ElBruno.NetAgent.Core.Configuration;
using ElBruno.NetAgent.Services.Network;
using Microsoft.Extensions.Logging.Abstractions;

namespace ElBruno.NetAgent.Tests
{
    public class NetworkQualityMonitorTests
    {
        private class FakeTester : INetworkQualityTester
        {
            private readonly int _latency;
            private readonly double _loss;

            public FakeTester(int latency, double loss)
            {
                _latency = latency;
                _loss = loss;
            }

            public Task<(int LatencyMs, double PacketLossPercent)> TestEndpointsAsync(string[] endpoints, CancellationToken cancellationToken)
            {
                return Task.FromResult((_latency, _loss));
            }
        }

        [Fact]
        public async Task EvaluateAsync_Returns_Report_With_Sane_Score()
        {
            var adapter = new NetworkInterfaceInfo { Id = "if1", Name = "eth0", Description = "Intel" };
            var options = new NetAgentOptions(); // defaults: DryRunMode true, AutoModeDisabled false
            var tester = new FakeTester(latency: 300, loss: 0.0);
            var logger = new NullLogger<NetworkQualityMonitor>();

            var monitor = new NetworkQualityMonitor(tester, options, logger);
            var report = await monitor.EvaluateAsync(adapter, CancellationToken.None);

            Assert.Equal(300, report.LatencyMs);
            Assert.Equal(0.0, report.PacketLossPercent);
            Assert.InRange(report.Score, 0.0, 100.0);
            // High latency should reduce score below 100.
            Assert.True(report.Score < 100.0);

            // Options defaults preserved
            Assert.True(options.DryRunMode);
            Assert.False(options.AutoModeEnabled);
        }

        [Fact]
        public async Task EvaluateAsync_Reflects_PacketLoss_In_Score_And_Health()
        {
            var adapter = new NetworkInterfaceInfo { Id = "if2", Name = "wifi0", Description = "WiFi" };
            var options = new NetAgentOptions();
            var tester = new FakeTester(latency: 50, loss: 10.0); // 10% loss
            var logger = new NullLogger<NetworkQualityMonitor>();

            var monitor = new NetworkQualityMonitor(tester, options, logger);
            var report = await monitor.EvaluateAsync(adapter, CancellationToken.None);

            Assert.Equal(50, report.LatencyMs);
            Assert.Equal(10.0, report.PacketLossPercent);
            // With packet loss above default threshold, IsHealthy should be false
            Assert.False(report.IsHealthy);
            Assert.InRange(report.Score, 0.0, 100.0);
        }
    }
}
