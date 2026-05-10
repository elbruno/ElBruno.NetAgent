using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ElBruno.NetAgent.Core.Configuration;

namespace ElBruno.NetAgent.ViewModels
{
    public class SettingsViewModel : ElBruno.NetAgent.Interfaces.ISettingsViewModel
    {
        private readonly Core.Configuration.IConfigurationService _configurationService;
        private readonly NetAgentOptions _options;

        public event PropertyChangedEventHandler? PropertyChanged;

        // Simple relay command implementation for tests
        private class RelayCommand : ICommand
        {
            private readonly Action<object?> _action;
            public RelayCommand(Action<object?> action) => _action = action;
#pragma warning disable CS0067
            public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
            public bool CanExecute(object? parameter) => true;
            public void Execute(object? parameter) => _action(parameter);
        }

        public SettingsViewModel(Core.Configuration.IConfigurationService configurationService)
        {
            _configurationService = configurationService;
            _options = _configurationService.GetOptionsAsync(CancellationToken.None).GetAwaiter().GetResult() ?? new NetAgentOptions();

            // Map from options/switching rules to viewmodel properties
            var s = _options.SwitchingRules ?? new NetAgentOptions.SwitchingRulesOptions();
            DryRunMode = s.DryRunMode;
            AutoModeEnabled = s.AutoModeEnabled;
            AutoModeIntervalSeconds = s.AutoModeIntervalSeconds;
            IgnoreVirtualAdapters = s.IgnoreVirtualAdapters;
            ExcludedInterfaceKinds = s.ExcludedInterfaceKinds?.ToList() ?? new System.Collections.Generic.List<string>();
            PreferredInterfacePatternsText = string.Join(',', s.PreferredInterfacePatterns ?? new string[0]);
            ExcludedInterfacePatternsText = string.Join(',', s.ExcludedInterfacePatterns ?? new string[0]);

            ConfigFilePath = _configurationService.GetConfigFilePath();
            AppDataFolder = _configurationService.GetConfigFolderPath();

            SaveCommand = new RelayCommand(async _ => await SaveAsync());
            ReloadCommand = new RelayCommand(_ => Reload());
            ResetCommand = new RelayCommand(_ => ResetToDefaults());
        }

        private async Task SaveAsync()
        {
            // Normalize values
            if (AutoModeIntervalSeconds < 10) AutoModeIntervalSeconds = NetAgentOptions.DefaultCheckIntervalSeconds;

            var switching = new NetAgentOptions.SwitchingRulesOptions
            {
                DryRunMode = this.DryRunMode,
                AutoModeEnabled = this.AutoModeEnabled,
                AutoModeIntervalSeconds = this.AutoModeIntervalSeconds,
                IgnoreVirtualAdapters = this.IgnoreVirtualAdapters,
                PreferredInterfacePatterns = (PreferredInterfacePatternsText ?? string.Empty).Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries),
                ExcludedInterfacePatterns = (ExcludedInterfacePatternsText ?? string.Empty).Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries),
                ExcludedInterfaceKinds = this.ExcludedInterfaceKinds?.ToArray() ?? new string[0]
            };

            _options.SwitchingRules = switching;
            await _configuration_service_save_options_async(_options).ConfigureAwait(false);

            OnPropertyChanged(nameof(DryRunMode));
            OnPropertyChanged(nameof(AutoModeEnabled));
            OnPropertyChanged(nameof(AutoModeIntervalSeconds));
        }

        // small wrapper to call SaveOptionsAsync with compatibility for older interfaces
        private Task _configuration_service_save_options_async(NetAgentOptions opts)
        {
            return _configurationService.SaveOptionsAsync(opts, CancellationToken.None);
        }

        private void Reload()
        {
            var opts = _configurationService.ReloadAsync(CancellationToken.None).GetAwaiter().GetResult();
            var s = opts.SwitchingRules ?? new NetAgentOptions.SwitchingRulesOptions();
            DryRunMode = s.DryRunMode;
            AutoModeEnabled = s.AutoModeEnabled;
            AutoModeIntervalSeconds = s.AutoModeIntervalSeconds;
            IgnoreVirtualAdapters = s.IgnoreVirtualAdapters;
            ExcludedInterfaceKinds = s.ExcludedInterfaceKinds?.ToList() ?? new System.Collections.Generic.List<string>();
            PreferredInterfacePatternsText = string.Join(',', s.PreferredInterfacePatterns ?? new string[0]);
            ExcludedInterfacePatternsText = string.Join(',', s.ExcludedInterfacePatterns ?? new string[0]);

            OnPropertyChanged(nameof(DryRunMode));
            OnPropertyChanged(nameof(AutoModeEnabled));
        }

        private void ResetToDefaults()
        {
            var s = new NetAgentOptions.SwitchingRulesOptions();
            DryRunMode = s.DryRunMode;
            AutoModeEnabled = s.AutoModeEnabled;
            AutoModeIntervalSeconds = s.AutoModeIntervalSeconds;
            IgnoreVirtualAdapters = s.IgnoreVirtualAdapters;
            ExcludedInterfaceKinds = s.ExcludedInterfaceKinds?.ToList() ?? new System.Collections.Generic.List<string>();
            PreferredInterfacePatternsText = string.Join(',', s.PreferredInterfacePatterns ?? new string[0]);
            ExcludedInterfacePatternsText = string.Join(',', s.ExcludedInterfacePatterns ?? new string[0]);

            OnPropertyChanged(nameof(DryRunMode));
            OnPropertyChanged(nameof(AutoModeEnabled));
        }

        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ViewModel properties
        public bool DryRunMode { get; set; } = true;
        public bool AutoModeEnabled { get; set; } = false;
        public int AutoModeIntervalSeconds { get; set; } = NetAgentOptions.DefaultCheckIntervalSeconds;
        public bool IgnoreVirtualAdapters { get; set; } = true;
        public System.Collections.Generic.List<string> ExcludedInterfaceKinds { get; set; } = new System.Collections.Generic.List<string>();
        public string PreferredInterfacePatternsText { get; set; } = string.Empty;
        public string ExcludedInterfacePatternsText { get; set; } = string.Empty;

        public string ConfigFilePath { get; private set; } = string.Empty;
        public string AppDataFolder { get; private set; } = string.Empty;

        public ICommand SaveCommand { get; }
        public ICommand ReloadCommand { get; }
        public ICommand ResetCommand { get; }
    }
}