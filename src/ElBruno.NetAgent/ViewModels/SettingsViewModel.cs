using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using ElBruno.NetAgent.Core.Configuration;

namespace ElBruno.NetAgent.ViewModels
{
    public class SettingsViewModel : ElBruno.NetAgent.Interfaces.ISettingsViewModel
    {
        private readonly Core.Configuration.IConfigurationService _configurationService;
        private NetAgentOptions _options;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool DryRunMode { get; set; } = true;
        public bool AutoModeEnabled { get; set; } = false;
        public int AutoModeIntervalSeconds { get; set; } = NetAgentOptions.DefaultCheckIntervalSeconds;
        public int PauseDurationSeconds { get; set; } = 0;
        public double MinQualityScore { get; set; } = 0.0;
        public double MinScoreImprovement { get; set; } = 5.0;
        public string PreferredInterfacePatternsText { get; set; } = string.Empty; // newline separated
        public string ExcludedInterfacePatternsText { get; set; } = string.Empty;
        public bool PreferUsbTethering { get; set; } = false;
        public bool AllowWifi { get; set; } = true;
        public bool AllowEthernet { get; set; } = true;
        public bool IgnoreVirtualAdapters { get; set; } = true;
        public bool IgnoreDownAdapters { get; set; } = true;

        public string ConfigFilePath { get; private set; } = string.Empty;
        public string AppDataFolder { get; private set; } = string.Empty;

        public ICommand SaveCommand { get; }
        public ICommand ReloadCommand { get; }
        public ICommand ResetCommand { get; }

        public SettingsViewModel(Core.Configuration.IConfigurationService configurationService)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _options = _configurationService.GetOptionsAsync(System.Threading.CancellationToken.None).GetAwaiter().GetResult() ?? new NetAgentOptions();

            MapFromOptions(_options);

            ConfigFilePath = _configurationService.GetConfigFilePath();
            AppDataFolder = _configurationService.GetConfigFolderPath();

            SaveCommand = new DelegateCommand(async _ => await SaveAsync());
            ReloadCommand = new DelegateCommand(async _ => await ReloadAsync());
            ResetCommand = new DelegateCommand(_ => ResetToDefaults());

            OnPropertyChanged(nameof(ConfigFilePath));
            OnPropertyChanged(nameof(AppDataFolder));
        }

        private void MapFromOptions(NetAgentOptions opts)
        {
            if (opts == null) opts = new NetAgentOptions();
            DryRunMode = opts.DryRunMode;
            AutoModeEnabled = opts.AutoModeEnabled;
            AutoModeIntervalSeconds = opts.AutoModeIntervalSeconds;
            var sr = opts.SwitchingRules ?? new SwitchingRules();
            PauseDurationSeconds = sr.PauseDurationSeconds;
            MinQualityScore = sr.MinQualityScore;
            MinScoreImprovement = sr.MinScoreImprovement;
            PreferredInterfacePatternsText = string.Join(Environment.NewLine, sr.PreferredInterfacePatterns ?? new System.Collections.Generic.List<string>());
            ExcludedInterfacePatternsText = string.Join(Environment.NewLine, sr.ExcludedInterfacePatterns ?? new System.Collections.Generic.List<string>());
            PreferUsbTethering = sr.PreferUsbTethering;
            AllowWifi = sr.AllowWifi;
            AllowEthernet = sr.AllowEthernet;
            IgnoreVirtualAdapters = sr.IgnoreVirtualAdapters;
            IgnoreDownAdapters = sr.IgnoreDownAdapters;

            _options = opts;
            RaiseAllChanged();
        }

        private void MapToOptions()
        {
            if (_options == null) _options = new NetAgentOptions();
            _options.DryRunMode = DryRunMode;
            _options.AutoModeEnabled = AutoModeEnabled;
            _options.AutoModeIntervalSeconds = Math.Max(1, AutoModeIntervalSeconds);
            if (_options.SwitchingRules == null) _options.SwitchingRules = new SwitchingRules();
            var sr = _options.SwitchingRules;
            sr.PauseDurationSeconds = Math.Max(0, PauseDurationSeconds);
            sr.MinQualityScore = Math.Clamp(MinQualityScore, 0.0, 100.0);
            sr.MinScoreImprovement = Math.Max(0.0, MinScoreImprovement);
            sr.PreferredInterfacePatterns = (PreferredInterfacePatternsText ?? string.Empty).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            sr.ExcludedInterfacePatterns = (ExcludedInterfacePatternsText ?? string.Empty).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            sr.PreferUsbTethering = PreferUsbTethering;
            sr.AllowWifi = AllowWifi;
            sr.AllowEthernet = AllowEthernet;
            sr.IgnoreVirtualAdapters = IgnoreVirtualAdapters;
            sr.IgnoreDownAdapters = IgnoreDownAdapters;
        }

        private async System.Threading.Tasks.Task SaveAsync()
        {
            MapToOptions();
            try
            {
                await _configurationService.SaveAsync(_options).ConfigureAwait(false);
            }
            catch
            {
                // swallow - UI may show errors in future
            }
        }

        private async System.Threading.Tasks.Task ReloadAsync()
        {
            var opts = await _configurationService.ReloadAsync(System.Threading.CancellationToken.None).ConfigureAwait(false);
            MapFromOptions(opts ?? new NetAgentOptions());
        }

        private void ResetToDefaults()
        {
            _options = new NetAgentOptions();
            MapFromOptions(_options);
        }

        private void RaiseAllChanged()
        {
            OnPropertyChanged(nameof(DryRunMode));
            OnPropertyChanged(nameof(AutoModeEnabled));
            OnPropertyChanged(nameof(AutoModeIntervalSeconds));
            OnPropertyChanged(nameof(PauseDurationSeconds));
            OnPropertyChanged(nameof(MinQualityScore));
            OnPropertyChanged(nameof(MinScoreImprovement));
            OnPropertyChanged(nameof(PreferredInterfacePatternsText));
            OnPropertyChanged(nameof(ExcludedInterfacePatternsText));
            OnPropertyChanged(nameof(PreferUsbTethering));
            OnPropertyChanged(nameof(AllowWifi));
            OnPropertyChanged(nameof(AllowEthernet));
            OnPropertyChanged(nameof(IgnoreVirtualAdapters));
            OnPropertyChanged(nameof(IgnoreDownAdapters));
        }

        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private class DelegateCommand : ICommand
        {
            private readonly Action<object?> _execute;
            public DelegateCommand(Action<object?> execute) => _execute = execute;
#pragma warning disable CS0067
            public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
            public bool CanExecute(object? parameter) => true;
            public void Execute(object? parameter) => _execute(parameter);
        }
    }
}
