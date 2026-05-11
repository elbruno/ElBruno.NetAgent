using System;
using System.Linq;
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
        public void SettingsViewModel_ExposesPropertiesAndConfigPaths()
        {
            var opts = new NetAgentOptions()
            {
                SwitchingRules = new NetAgentOptions.SwitchingRulesOptions
                {
                    DryRunMode = false,
                    AutoModeEnabled = true,
                    AutoModeIntervalSeconds = 45,
                    PauseAutoSwitchDurationSeconds = 15,
                    MinimumQualityScore = 12.5,
                    MinimumScoreImprovement = 3.3,
                    IgnoreVirtualAdapters = false,
                    ExcludedInterfaceKinds = new[] { "Loopback", "CustomKind" },
                    PreferredInterfacePatterns = new[] { "p1" },
                    ExcludedInterfacePatterns = new[] { "e1" },
                    PreferUsbTethering = true,
                    AllowWiFi = false,
                    AllowEthernet = false
                }
            };

            var fake = new FakeConfigService(opts);
            var vm = new SettingsViewModel(fake);

            Assert.False(vm.DryRunMode);
            Assert.True(vm.AutoModeEnabled);
            Assert.Equal(45, vm.AutoModeIntervalSeconds);
            Assert.Equal(15, vm.PauseAutoSwitchDurationSeconds);
            Assert.Equal(12.5, vm.MinimumQualityScoreRequired);
            Assert.Equal(3.3, vm.MinimumScoreImprovementRequired);
            Assert.False(vm.IgnoreVirtualAdapters);
            Assert.Contains("CustomKind", vm.ExcludedInterfaceKinds);
            Assert.Contains("p1", vm.PreferredInterfacePatternsText.Split(',').Select(s => s.Trim()));
            Assert.Contains("e1", vm.ExcludedInterfacePatternsText.Split(',').Select(s => s.Trim()));
            Assert.True(vm.PreferUsbTethering);
            Assert.False(vm.AllowWifi);
            Assert.False(vm.AllowEthernet);
            Assert.Equal("C:\\temp\\config.json", vm.ConfigFilePath);
            Assert.Equal("C:\\temp", vm.AppDataFolder);
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
            var saved = fake.LastSavedOptions!.SwitchingRules!;
            Assert.False(saved.DryRunMode);
            Assert.True(saved.AutoModeEnabled);
            Assert.Equal(20, saved.AutoModeIntervalSeconds);
            Assert.Contains("eth0", saved.PreferredInterfacePatterns);
            Assert.Contains("wlan0", saved.PreferredInterfacePatterns);
            Assert.Contains("vEthernet0", saved.ExcludedInterfacePatterns);
        }

        [Fact]
        public void SettingsViewModel_Save_PersistsExcludedKindsAndFlags()
        {
            var opts = new NetAgentOptions();
            var fake = new FakeConfigService(opts);
            var vm = new SettingsViewModel(fake);

            // Remove Loopback from explicit list and add a custom kind; keep Vpn flag enabled
            vm.ExcludedInterfaceKinds.Remove("Loopback");
            vm.ExcludedInterfaceKinds.Add("CustomKind");
            vm.IgnoreLoopbackAdapters = false; // should not add Loopback when saving
            vm.IgnoreVpnAdapters = true; // should add Vpn when saving

            vm.SaveCommand.Execute(null);

            Assert.NotNull(fake.LastSavedOptions);
            var saved = fake.LastSavedOptions!.SwitchingRules!;
            Assert.Contains("CustomKind", saved.ExcludedInterfaceKinds);
            Assert.Contains("Vpn", saved.ExcludedInterfaceKinds);
            Assert.DoesNotContain("Loopback", saved.ExcludedInterfaceKinds);
        }

        [Fact]
        public void SettingsViewModel_ReloadUpdatesProperties()
        {
            var initial = new NetAgentOptions();
            var fake = new FakeConfigService(initial);
            var vm = new SettingsViewModel(fake);

            // Change underlying options and simulate reload
            fake.OptionsToReturn = new NetAgentOptions
            {
                SwitchingRules = new NetAgentOptions.SwitchingRulesOptions
                {
                    DryRunMode = false,
                    AutoModeEnabled = true,
                    AutoModeIntervalSeconds = 99
                }
            };

            vm.ReloadCommand.Execute(null);

            Assert.False(vm.DryRunMode);
            Assert.True(vm.AutoModeEnabled);
            Assert.Equal(99, vm.AutoModeIntervalSeconds);
        }

        [Fact]
        public void SettingsViewModel_ResetResetsToDefaults()
        {
            var opts = new NetAgentOptions();
            var fake = new FakeConfigService(opts);
            var vm = new SettingsViewModel(fake);

            // Change some properties away from defaults
            vm.DryRunMode = false;
            vm.AutoModeEnabled = true;
            vm.AutoModeIntervalSeconds = 120;

            vm.ResetCommand.Execute(null);

            var defaults = new NetAgentOptions.SwitchingRulesOptions();
            Assert.Equal(defaults.DryRunMode, vm.DryRunMode);
            Assert.Equal(defaults.AutoModeEnabled, vm.AutoModeEnabled);
            Assert.Equal(defaults.AutoModeIntervalSeconds, vm.AutoModeIntervalSeconds);
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
            Assert.NotNull(fake.LastSavedOptions!.SwitchingRules);
            var saved = fake.LastSavedOptions!.SwitchingRules!;
            Assert.Equal(NetAgentOptions.DefaultCheckIntervalSeconds, saved.AutoModeIntervalSeconds);
        }
    }
}
