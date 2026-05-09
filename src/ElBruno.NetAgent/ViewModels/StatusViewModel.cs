using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ElBruno.NetAgent.Core.Configuration;
using ElBruno.NetAgent.Core.Models;
using ElBruno.NetAgent.Core.Services;
using ElBruno.NetAgent.Interfaces;

namespace ElBruno.NetAgent.ViewModels
{
    public class StatusViewModel : IStatusViewModel
    {
        private readonly INetworkInventoryService _inventory;
        private readonly INetworkQualityMonitor _qualityMonitor;
        private readonly IDialogService _dialogService;
        private readonly NetAgentOptions _options;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string CurrentInterface { get; private set; } = string.Empty;
        public ObservableCollection<string> DetectedInterfaces { get; private set; } = new ObservableCollection<string>();
        public double Latency { get; private set; }
        public double LatencyAverage => Latency;
        public double QualityScore { get; private set; }
        public bool AutoMode { get; private set; }
        public string LastDecision { get; private set; } = string.Empty;
        public string LastSwitchTimestamp { get; private set; } = string.Empty;

        public ICommand RefreshCommand { get; }
        public ICommand OpenLogsCommand { get; }
        public ICommand OpenConfigCommand { get; }
        public ICommand RestoreMetricsCommand { get; }

        public StatusViewModel(INetworkInventoryService inventory, INetworkQualityMonitor qualityMonitor, IDialogService dialogService, NetAgentOptions options)
        {
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _qualityMonitor = qualityMonitor ?? throw new ArgumentNullException(nameof(qualityMonitor));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _options = options ?? throw new ArgumentNullException(nameof(options));

            AutoMode = _options.AutoModeEnabled;

            RefreshCommand = new DelegateCommand(_ => Refresh());
            OpenLogsCommand = new DelegateCommand(_ => _dialogService.OpenLogs());
            OpenConfigCommand = new DelegateCommand(_ => _dialogService.OpenConfig());
            RestoreMetricsCommand = new DelegateCommand(_ => RestoreMetrics());
        }

        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public Task RefreshAsync()
        {
            return Task.Run(() => Refresh());
        }

        private void Refresh()
        {
            var list = _inventory.GetInterfacesAsync(CancellationToken.None).GetAwaiter().GetResult();
            DetectedInterfaces = new ObservableCollection<string>(list.Select(i => i.Name));
            OnPropertyChanged(nameof(DetectedInterfaces));

            if (list.Count > 0)
            {
                var cur = list[0];
                CurrentInterface = cur.Name;
                OnPropertyChanged(nameof(CurrentInterface));

                var report = _qualityMonitor.EvaluateAsync(cur, CancellationToken.None).GetAwaiter().GetResult();
                Latency = report.LatencyMs;
                QualityScore = report.Score;
                LastDecision = "Refreshed";
                OnPropertyChanged(nameof(Latency));
                OnPropertyChanged(nameof(QualityScore));
                OnPropertyChanged(nameof(LastDecision));
            }
        }

        private void RestoreMetrics()
        {
            LastDecision = "MetricsRestored";
            OnPropertyChanged(nameof(LastDecision));
        }

        private class DelegateCommand : ICommand
        {
            private readonly Action<object?> _execute;
            public DelegateCommand(Action<object?> execute) => _execute = execute;
            public event EventHandler? CanExecuteChanged;
            public bool CanExecute(object? parameter) => true;
            public void Execute(object? parameter) => _execute(parameter);
        }
    }
}
