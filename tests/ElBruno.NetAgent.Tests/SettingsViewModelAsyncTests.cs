using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Xunit;
using ElBruno.NetAgent.ViewModels;
using ElBruno.NetAgent.Core.Configuration;
using ElBruno.NetAgent.Services;
using ElBruno.NetAgent.Core.Services;
using ElBruno.NetAgent.Core.Decision;
using ElBruno.NetAgent.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace ElBruno.NetAgent.Tests
{
    public class SettingsViewModelAsyncTests
    {
        private class DelayedFakeConfigService : IConfigurationService
        {
            public NetAgentOptions OptionsToReturn { get; set; }
            public NetAgentOptions? LastSavedOptions { get; private set; }

            public TaskCompletionSource<bool> SaveStarted { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource<bool> SaveCompleted { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            private readonly int _delayMs;

            public DelayedFakeConfigService(NetAgentOptions opts, int delayMs = 200)
            {
                OptionsToReturn = opts;
                _delayMs = delayMs;
            }

            public Task<NetAgentOptions> GetOptionsAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(OptionsToReturn);
            }

            public Task<NetAgentOptions> ReloadAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(OptionsToReturn);
            }

            public async Task SaveOptionsAsync(NetAgentOptions options, CancellationToken cancellationToken = default)
            {
                SaveStarted.TrySetResult(true);
                // Simulate asynchronous work that should not block the caller of RelayCommand.Execute (which discards the task)
                await Task.Delay(_delayMs, cancellationToken).ConfigureAwait(false);
                LastSavedOptions = options;
                SaveCompleted.TrySetResult(true);
            }

            public string GetConfigFolderPath() => "C:\\temp";
            public string GetConfigFilePath() => "C:\\temp\\config.json";
            public void OpenConfigFolder() { }
            public void OpenConfigFile() { }
        }

        [Fact]
        public async Task SaveCommand_CompletesInBackground_DoesNotBlockInvoker()
        {
            var shared = new NetAgentOptions();
            var fake = new DelayedFakeConfigService(shared, delayMs: 300);
            var vm = new SettingsViewModel(fake);

            // Measure Execute(...) return time - it should not block while save runs
            var sw = Stopwatch.StartNew();
            vm.SaveCommand.Execute(null);
            sw.Stop();

            Assert.True(sw.ElapsedMilliseconds < 200, $"SaveCommand.Execute blocked the caller for {sw.ElapsedMilliseconds}ms");

            // Verify the save actually completed within a reasonable timeout (2s)
            var completedTask = await Task.WhenAny(fake.SaveCompleted.Task, Task.Delay(2000));
            Assert.Equal(fake.SaveCompleted.Task, completedTask);
        }

        private class FakeInventory : INetworkInventoryService
        {
            public bool Called { get; private set; }
            public Task<IReadOnlyList<NetworkInterfaceInfo>> GetInterfacesAsync(CancellationToken cancellationToken = default)
            {
                Called = true;
                IReadOnlyList<NetworkInterfaceInfo> empty = Array.Empty<NetworkInterfaceInfo>();
                return Task.FromResult(empty);
            }
        }

        private class DummyQualityMonitor : INetworkQualityMonitor
        {
            public Task<NetworkQualityReport> EvaluateAsync(NetworkInterfaceInfo adapter, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new NetworkQualityReport { InterfaceId = adapter.Id, LatencyMs = 0, PacketLossPercent = 0.0 });
            }
        }

        private class DummyDecisionEngine : IDecisionEngine
        {
            public Task<Core.Decision.DecisionResult> EvaluateAsync(NetworkQualityReport report, NetAgentOptions options, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new Core.Decision.DecisionResult(Core.Decision.DecisionAction.None, "test"));
            }
        }

        [Fact]
        public async Task AutoModeHostedService_RemainsIdle_WhenAutoModeDisabledAfterSave()
        {
            // Shared options returned by config service and used by IOptions wrapper
            var shared = new NetAgentOptions();
            shared.SwitchingRules = new NetAgentOptions.SwitchingRulesOptions { AutoModeEnabled = false };
            shared.AutoModeEnabled = false;

            var fakeConfig = new DelayedFakeConfigService(shared, delayMs: 10);
            var vm = new SettingsViewModel(fakeConfig);

            // Ensure AutoModeDisabled and persist via Save
            vm.AutoModeEnabled = false;
            vm.SaveCommand.Execute(null);
            // Wait for save to complete to ensure _options instance was updated by VM
            var completed = await Task.WhenAny(fakeConfig.SaveCompleted.Task, Task.Delay(2000));
            Assert.Equal(fakeConfig.SaveCompleted.Task, completed);

            var inventory = new FakeInventory();
            var quality = new DummyQualityMonitor();
            var decision = new DummyDecisionEngine();
            var logger = new LoggerFactory().CreateLogger<AutoModeHostedService>();
            var provider = new ServiceCollection()
                .AddSingleton<INetworkController>(new DummyNetworkController())
                .BuildServiceProvider();

            var options = new OptionsWrapper<NetAgentOptions>(shared);
            var svc = new AutoModeHostedService(logger, inventory, quality, decision, provider, options);

            // Run single evaluation - should return quickly and not call inventory when AutoMode is disabled
            await svc.EvaluateOnceAsync(CancellationToken.None);
            Assert.False(inventory.Called, "Inventory should not be queried when AutoMode is disabled");
        }

        // Minimal dummy controller used only if service attempts to resolve controller - not expected in these tests
        private class DummyNetworkController : INetworkController
        {
            public Task<string> PreferInterfaceAsync(string interfaceId, CancellationToken cancellationToken = default) => Task.FromResult("ok");
            public Task<string> RestoreAutomaticMetricsAsync(CancellationToken cancellationToken = default) => Task.FromResult("restored");
        }
    }
}
