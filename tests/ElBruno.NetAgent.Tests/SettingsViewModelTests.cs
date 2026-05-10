using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ElBruno.NetAgent.ViewModels;
using ElBruno.NetAgent.Core.Configuration;

namespace ElBruno.NetAgent.Tests
{
    public class SettingsViewModelTests
    {
        private class FakeConfigService : IConfigurationService
        {
            public NetAgentOptions OptionsToReturn { get; set; }
            public NetAgentOptions? LastSavedOptions { get; private set; }

            public FakeConfigService(NetAgentOptions opts)
            {
                OptionsToReturn = opts;
            }

            public Task<NetAgentOptions> GetOptionsAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(OptionsToReturn);
            }

            public Task<NetAgentOptions> ReloadAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(OptionsToReturn);
            }

            public Task SaveOptionsAsync(NetAgentOptions options, CancellationToken cancellationToken = default)
            {
                LastSavedOptions = options;
                return Task.CompletedTask;
            }

            public string GetConfigFolderPath() => "C:\\temp";
            public string GetConfigFilePath() => "C:\\temp\\config.json";
            public void OpenConfigFolder() { }
            public void OpenConfigFile() { }
        }

        [Fact]
        public void SettingsViewModel_LoadsDefaults()
        {
            var opts = new NetAgentOptions();
            var fake = new FakeConfigService(opts);
            var vm = new SettingsViewModel(fake);

            Assert.True(vm.DryRunMode, "DryRun default should be true");
            Assert.True(vm.IgnoreVirtualAdapters, "IgnoreVirtualAdapters default should be true");
            Assert.Contains("Loopback", vm.ExcludedInterfaceKinds);
            Assert.Contains("Vpn", vm.ExcludedInterfaceKinds);
        }

        [Fact]
        public void SettingsViewModel_SavePersists()
        {
            var opts = new NetAgentOptions();
            var fake = new FakeConfigService(opts);
            var vm = new SettingsViewModel(fake);

            vm.DryRunMode = false;
            vm.AutoModeEnabled = true;
            vm.AutoModeIntervalSeconds = 20;
            vm.PreferredInterfacePatternsText = "eth0,wlan0";
            vm.ExcludedInterfacePatternsText = "vEthernet0";

            vm.SaveCommand.Execute(null);

            Assert.NotNull(fake.LastSavedOptions);
            var saved = fake.LastSavedOptions!.SwitchingRules;
            Assert.False(saved.DryRunMode);
            Assert.True(saved.AutoModeEnabled);
            Assert.Equal(20, saved.AutoModeIntervalSeconds);
            Assert.Contains("eth0", saved.PreferredInterfacePatterns);
            Assert.Contains("wlan0", saved.PreferredInterfacePatterns);
            Assert.Contains("vEthernet0", saved.ExcludedInterfacePatterns);
        }

        [Fact]
        public void SettingsViewModel_InvalidValuesNormalized()
        {
            var opts = new NetAgentOptions();
            var fake = new FakeConfigService(opts);
            var vm = new SettingsViewModel(fake);

            vm.AutoModeIntervalSeconds = 5; // too small - should be normalized to default (DefaultCheckIntervalSeconds)
            vm.SaveCommand.Execute(null);

            Assert.NotNull(fake.LastSavedOptions);
            var saved = fake.LastSavedOptions!.SwitchingRules;
            Assert.Equal(NetAgentOptions.DefaultCheckIntervalSeconds, saved.AutoModeIntervalSeconds);
        }
    }
}
