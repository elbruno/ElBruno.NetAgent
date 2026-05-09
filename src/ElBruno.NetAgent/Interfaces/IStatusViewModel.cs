using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ElBruno.NetAgent.Interfaces
{
    public interface IStatusViewModel : INotifyPropertyChanged
    {
        string CurrentInterface { get; }
        ObservableCollection<string> DetectedInterfaces { get; }
        double LatencyAverage { get; }
        double QualityScore { get; }
        bool AutoMode { get; }
        string LastDecision { get; }
        string LastSwitchTimestamp { get; }

        ICommand RefreshCommand { get; }
        ICommand OpenLogsCommand { get; }
        ICommand OpenConfigCommand { get; }
        ICommand RestoreMetricsCommand { get; }

        Task RefreshAsync();
    }
}
