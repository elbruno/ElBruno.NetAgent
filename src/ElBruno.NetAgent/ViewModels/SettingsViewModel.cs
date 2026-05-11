using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Input;
using ElBruno.NetAgent.Core.Configuration;

namespace ElBruno.NetAgent.ViewModels
{
    public class SettingsViewModel : ElBruno.NetAgent.Interfaces.ISettingsViewModel
    {
        private readonly Core.Configuration.IConfigurationService _configurationService;
        private readonly Core.Services.IDialogService _dialogService;
        private readonly NetAgentOptions _options;

        public event PropertyChangedEventHandler? PropertyChanged;

        // Simple relay command implementation for tests
        private class RelayCommand : ICommand
        {
            private readonly System.Func<object?, System.Threading.Tasks.Task> _action;
            public RelayCommand(System.Func<object?, System.Threading.Tasks.Task> action) => _action = action;
            public RelayCommand(System.Action<object?> action) => _action = (p) => { action(p); return System.Threading.Tasks.Task.CompletedTask; };
#pragma warning disable CS0067
            public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
            public bool CanExecute(object? parameter) => true;
            public void Execute(object? parameter) => _action(parameter).GetAwaiter().GetResult();
        }

        public SettingsViewModel(Core.Configuration.IConfigurationService configurationService) : this(configurationService, new ElBruno.NetAgent.Services.NullDialogService()) { }

        public SettingsViewModel(Core.Configuration.IConfigurationService configurationService, Core.Services.IDialogService dialogService)
        {
            _configurationService = configurationService;
            _dialogService = dialogService;
            _options = _configurationService.GetOptionsAsync(CancellationToken.None).GetAwaiter().GetResult() ?? new NetAgentOptions();

            // Map from options/switching rules to viewmodel properties
            var s = _options.SwitchingRules ?? new NetAgentOptions.SwitchingRulesOptions();
            DryRunMode = s.DryRunMode;
            AutoModeEnabled = s.AutoModeEnabled;
            AutoModeIntervalSeconds = s.AutoModeIntervalSeconds;
            PauseAutoSwitchDurationSeconds = s.PauseAutoSwitchDurationSeconds;
            MinimumQualityScoreRequired = (double)System.Math.Max(0.0, s.MinimumQualityScore);
            MinimumScoreImprovementRequired = (double)System.Math.Max(0.0, s.MinimumScoreImprovement);

            IgnoreVirtualAdapters = s.IgnoreVirtualAdapters;
            // Excluded kinds list and individual flags
            ExcludedInterfaceKinds = s.ExcludedInterfaceKinds?.ToList() ?? new System.Collections.Generic.List<string>();
            IgnoreLoopbackAdapters = ExcludedInterfaceKinds.Contains("Loopback");
            IgnoreVpnAdapters = ExcludedInterfaceKinds.Contains("Vpn");
            IgnoreDownOrUnknown = s.IgnoreDownOrUnknownAdapters;

            PreferredInterfacePatternsText = string.Join(',', s.PreferredInterfacePatterns ?? new string[0]);
            ExcludedInterfacePatternsText = string.Join(',', s.ExcludedInterfacePatterns ?? new string[0]);

            PreferUsbTethering = s.PreferUsbTethering;
            AllowWifi = s.AllowWiFi;
            AllowEthernet = s.AllowEthernet;

            ConfigFilePath = _configurationService.GetConfigFilePath();
            AppDataFolder = _configuration_service_get_config_folder_path();

            SaveCommand = new RelayCommand(async _ => await SaveAsync());
            ReloadCommand = new RelayCommand(_ => { Reload(); });
            ResetCommand = new RelayCommand(_ => { ResetToDefaults(); });

            OpenConfigCommand = new RelayCommand(_ => { _dialogService.OpenConfig(); });
            OpenAppDataCommand = new RelayCommand(_ => { _dialogService.OpenConfigFolder(); });
            OpenLogsCommand = new RelayCommand(_ => { _dialogService.OpenLogs(); });
        }

        private async Task SaveAsync()
        {
            // Normalize numeric values to safe ranges using locals to avoid mutation side-effects
            var interval = this.AutoModeIntervalSeconds;
            if (interval < 10) interval = NetAgentOptions.DefaultCheckIntervalSeconds; // normalize small values to default
            var pauseDuration = this.PauseAutoSwitchDurationSeconds < 0 ? 0 : this.PauseAutoSwitchDurationSeconds;
            var minQuality = this.MinimumQualityScoreRequired < 0 ? 0.0 : this.MinimumQualityScoreRequired;
            var minImprovement = this.MinimumScoreImprovementRequired < 0 ? 0.0 : this.MinimumScoreImprovementRequired;

            // Compose excluded kinds based on explicit flags and any additional kinds set in the UI
            var kinds = new System.Collections.Generic.List<string>();
            if (this.IgnoreLoopbackAdapters) kinds.Add("Loopback");
            if (this.IgnoreVpnAdapters) kinds.Add("Vpn");
            if (this.IgnoreVirtualAdapters) kinds.Add("Virtual");
            if (this.IgnoreDownOrUnknown) kinds.Add("Unknown");

            if (this.ExcludedInterfaceKinds != null)
            {
                foreach (var k in this.ExcludedInterfaceKinds)
                {
                    if (!string.Equals(k, "Loopback", System.StringComparison.OrdinalIgnoreCase) && !string.Equals(k, "Vpn", System.StringComparison.OrdinalIgnoreCase) && !string.Equals(k, "Virtual", System.StringComparison.OrdinalIgnoreCase) && !string.Equals(k, "Unknown", System.StringComparison.OrdinalIgnoreCase))
                        kinds.Add(k);
                }
            }

            var switching = new NetAgentOptions.SwitchingRulesOptions
            {
                DryRunMode = this.DryRunMode,
                AutoModeEnabled = this.AutoModeEnabled,
                AutoModeIntervalSeconds = interval,
                PauseAutoSwitchDurationSeconds = pauseDuration,
                MinimumQualityScore = minQuality,
                MinimumScoreImprovement = minImprovement,
                IgnoreVirtualAdapters = this.IgnoreVirtualAdapters,
                IgnoreDownOrUnknownAdapters = this.IgnoreDownOrUnknown,
                PreferredInterfacePatterns = (PreferredInterfacePatternsText ?? string.Empty).Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries),
                ExcludedInterfacePatterns = (ExcludedInterfacePatternsText ?? string.Empty).Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries),
                ExcludedInterfaceKinds = kinds.Distinct(System.StringComparer.OrdinalIgnoreCase).ToArray(),
                PreferUsbTethering = this.PreferUsbTethering,
                AllowWiFi = this.AllowWifi,
                AllowEthernet = this.AllowEthernet
            };

            // Do not directly persist the original _options instance to avoid shared-reference side effects in tests.
            // Debug: log the local interval and what we'll place into SwitchingRules and top-level AutoModeIntervalSeconds
            Debug.WriteLine($"SettingsViewModel.SaveAsync: local interval = {interval}");

            var toSave = new NetAgentOptions
            {
                LatencyThresholdMs = _options.LatencyThresholdMs,
                PacketLossThresholdPercent = _options.PacketLossThresholdPercent,
                CheckIntervalSeconds = _options.CheckIntervalSeconds,
                AutoModeIntervalSeconds = interval,
                FailoverDurationSeconds = _options.FailoverDurationSeconds,
                FailbackCooldownSeconds = _options.FailbackCooldownSeconds,
                MinimumChecksBeforeSwitch = _options.MinimumChecksBeforeSwitch,
                MinimumScoreDeltaToSwitch = _options.MinimumScoreDeltaToSwitch,
                AutoModeEnabled = _options.AutoModeEnabled,
                DryRunMode = _options.DryRunMode,
                TestEndpoints = _options.TestEndpoints,
                MaxHistoryItems = _options.MaxHistoryItems,
                SwitchingRules = switching
            };

            Debug.WriteLine($"SettingsViewModel.SaveAsync: toSave.SwitchingRules.AutoModeIntervalSeconds = {toSave.SwitchingRules?.AutoModeIntervalSeconds}; toSave.AutoModeIntervalSeconds = {toSave.AutoModeIntervalSeconds}");

            await _configuration_service_save_options_async(toSave).ConfigureAwait(false);

            // Update in-memory options instance for runtime consistency
            _options.SwitchingRules = switching;

            OnPropertyChanged(nameof(DryRunMode));
            OnPropertyChanged(nameof(AutoModeEnabled));
            OnPropertyChanged(nameof(AutoModeIntervalSeconds));
            OnPropertyChanged(nameof(IgnoreDownOrUnknown));
        }

        // small wrapper to call SaveOptionsAsync with compatibility for older interfaces
        private Task _configuration_service_save_options_async(NetAgentOptions opts)
        {
            return _configurationService.SaveOptionsAsync(opts, CancellationToken.None);
        }

        // wrapper for compatibility
        private string _configuration_service_get_config_folder_path()
        {
            return _configurationService.GetConfigFolderPath();
        }

        private void Reload()
        {
            var opts = _configurationService.ReloadAsync(CancellationToken.None).GetAwaiter().GetResult();
            var s = opts.SwitchingRules ?? new NetAgentOptions.SwitchingRulesOptions();
            DryRunMode = s.DryRunMode;
            AutoModeEnabled = s.AutoModeEnabled;
            AutoModeIntervalSeconds = s.AutoModeIntervalSeconds;
            PauseAutoSwitchDurationSeconds = s.PauseAutoSwitchDurationSeconds;
            MinimumQualityScoreRequired = s.MinimumQualityScore;
            MinimumScoreImprovementRequired = s.MinimumScoreImprovement;
            IgnoreVirtualAdapters = s.IgnoreVirtualAdapters;
            ExcludedInterfaceKinds = s.ExcludedInterfaceKinds?.ToList() ?? new System.Collections.Generic.List<string>();
            PreferredInterfacePatternsText = string.Join(',', s.PreferredInterfacePatterns ?? new string[0]);
            ExcludedInterfacePatternsText = string.Join(',', s.ExcludedInterfacePatterns ?? new string[0]);
            PreferUsbTethering = s.PreferUsbTethering;
            AllowWifi = s.AllowWiFi;
            AllowEthernet = s.AllowEthernet;

            OnPropertyChanged(nameof(DryRunMode));
            OnPropertyChanged(nameof(AutoModeEnabled));
            OnPropertyChanged(nameof(AutoModeIntervalSeconds));
        }

        private void ResetToDefaults()
        {
            var s = new NetAgentOptions.SwitchingRulesOptions();
            DryRunMode = s.DryRunMode;
            AutoModeEnabled = s.AutoModeEnabled;
            AutoModeIntervalSeconds = s.AutoModeIntervalSeconds;
            PauseAutoSwitchDurationSeconds = s.PauseAutoSwitchDurationSeconds;
            MinimumQualityScoreRequired = s.MinimumQualityScore;
            MinimumScoreImprovementRequired = s.MinimumScoreImprovement;
            IgnoreVirtualAdapters = s.IgnoreVirtualAdapters;
            ExcludedInterfaceKinds = s.ExcludedInterfaceKinds?.ToList() ?? new System.Collections.Generic.List<string>();
            PreferredInterfacePatternsText = string.Join(',', s.PreferredInterfacePatterns ?? new string[0]);
            ExcludedInterfacePatternsText = string.Join(',', s.ExcludedInterfacePatterns ?? new string[0]);
            PreferUsbTethering = s.PreferUsbTethering;
            AllowWifi = s.AllowWiFi;
            AllowEthernet = s.AllowEthernet;

            OnPropertyChanged(nameof(DryRunMode));
            OnPropertyChanged(nameof(AutoModeEnabled));
        }

        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ViewModel properties
        public bool DryRunMode { get; set; } = true;
        public bool AutoModeEnabled { get; set; } = false;
        public int AutoModeIntervalSeconds { get; set; } = NetAgentOptions.DefaultCheckIntervalSeconds;
        public int PauseAutoSwitchDurationSeconds { get; set; } = 0;
        public double MinimumQualityScoreRequired { get; set; } = 0.0;
        public double MinimumScoreImprovementRequired { get; set; } = 0.0;

        public bool IgnoreVirtualAdapters { get; set; } = true;
        public bool IgnoreLoopbackAdapters { get; set; } = true;
        public bool IgnoreVpnAdapters { get; set; } = true;
        public bool IgnoreDownOrUnknown { get; set; } = true;
        public System.Collections.Generic.List<string> ExcludedInterfaceKinds { get; set; } = new System.Collections.Generic.List<string>();
        public string PreferredInterfacePatternsText { get; set; } = string.Empty;
        public string ExcludedInterfacePatternsText { get; set; } = string.Empty;

        public bool PreferUsbTethering { get; set; } = false;
        public bool AllowWifi { get; set; } = true;
        public bool AllowEthernet { get; set; } = true;

        public string ConfigFilePath { get; private set; } = string.Empty;
        public string AppDataFolder { get; private set; } = string.Empty;

        public ICommand SaveCommand { get; }
        public ICommand ReloadCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand OpenConfigCommand { get; }
        public ICommand OpenAppDataCommand { get; }
        public ICommand OpenLogsCommand { get; }
    }
}