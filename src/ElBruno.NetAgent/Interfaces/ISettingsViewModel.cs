using System.ComponentModel;

namespace ElBruno.NetAgent.Interfaces
{
    public interface ISettingsViewModel : INotifyPropertyChanged
    {
        bool DryRunMode { get; }
        bool AutoModeEnabled { get; }
        string ConfigFilePath { get; }
        string AppDataFolder { get; }
    }
}