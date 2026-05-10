using System.ComponentModel;

namespace ElBruno.NetAgent.Interfaces
{
    public interface ISettingsViewModel : INotifyPropertyChanged
    {
        bool DryRunMode { get; set; }
        bool AutoModeEnabled { get; set; }
        int AutoModeIntervalSeconds { get; set; }
        int PauseDurationSeconds { get; set; }
        double MinQualityScore { get; set; }
        double MinScoreImprovement { get; set; }
        string PreferredInterfacePatternsText { get; set; }
        string ExcludedInterfacePatternsText { get; set; }
        bool PreferUsbTethering { get; set; }
        bool AllowWifi { get; set; }
        bool AllowEthernet { get; set; }
        bool IgnoreVirtualAdapters { get; set; }
        bool IgnoreDownAdapters { get; set; }

        string ConfigFilePath { get; }
        string AppDataFolder { get; }

        System.Windows.Input.ICommand SaveCommand { get; }
        System.Windows.Input.ICommand ReloadCommand { get; }
        System.Windows.Input.ICommand ResetCommand { get; }
    }
}