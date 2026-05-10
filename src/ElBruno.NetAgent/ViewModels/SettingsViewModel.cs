using System.ComponentModel;
using ElBruno.NetAgent.Core.Configuration;

namespace ElBruno.NetAgent.ViewModels
{
    public class SettingsViewModel : ElBruno.NetAgent.Interfaces.ISettingsViewModel
    {
        private readonly Core.Configuration.IConfigurationService _configurationService;
        private readonly NetAgentOptions _options;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool DryRunMode { get; private set; } = true;
        public bool AutoModeEnabled { get; private set; } = false;
        public string ConfigFilePath { get; private set; } = string.Empty;
        public string AppDataFolder { get; private set; } = string.Empty;

        public SettingsViewModel(Core.Configuration.IConfigurationService configurationService)
        {
            _configurationService = configurationService;
            _options = _configurationService.GetOptionsAsync(System.Threading.CancellationToken.None).GetAwaiter().GetResult() ?? new NetAgentOptions();
            DryRunMode = _options.DryRunMode;
            AutoModeEnabled = _options.AutoModeEnabled;
            ConfigFilePath = _configurationService.GetConfigFilePath();
            AppDataFolder = _configurationService.GetConfigFolderPath();

            // Raise property changed for initial values to satisfy INotifyPropertyChanged consumers and avoid CS0067 warnings.
            OnPropertyChanged(nameof(DryRunMode));
            OnPropertyChanged(nameof(AutoModeEnabled));
            OnPropertyChanged(nameof(ConfigFilePath));
            OnPropertyChanged(nameof(AppDataFolder));
        }

        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}