using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ElBruno.NetAgent.Core.Models;
using ElBruno.NetAgent.Core.Services;
using ElBruno.NetAgent.Core.Decision;
using ElBruno.NetAgent.Core.Configuration;

namespace ElBruno.NetAgent.ViewModels
{
    public class NetworkSelectorViewModel : ElBruno.NetAgent.Interfaces.INetworkSelectorViewModel, INotifyPropertyChanged
    {
        private readonly INetworkInventoryService _inventory;
        private readonly INetworkQualityMonitor _qualityMonitor;
        private readonly IDecisionEngine _decisionEngine;
        private readonly NetAgentOptions _options;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<object> Interfaces { get; private set; } = new ObservableCollection<object>();
        public string LastActionMessage { get; private set; } = string.Empty;

        public ICommand RefreshCommand { get; }
        public ICommand RequestSwitchCommand { get; }

        public NetworkSelectorViewModel(INetworkInventoryService inventory, INetworkQualityMonitor qualityMonitor, IDecisionEngine decisionEngine, NetAgentOptions options)
        {
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _qualityMonitor = qualityMonitor ?? throw new ArgumentNullException(nameof(qualityMonitor));
            _decisionEngine = decisionEngine ?? throw new ArgumentNullException(nameof(decisionEngine));
            _options = options ?? new NetAgentOptions();

            RefreshCommand = new DelegateCommand(async _ => await RefreshAsync());
            RequestSwitchCommand = new DelegateCommand(async p =>
            {
                if (p is AdapterItem item)
                {
                    await RequestSwitchDryRunAsync(item);
                }
            });
        }

        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public Task RefreshAsync()
        {
            return Task.Run(() =>
            {
                var list = _inventory.GetInterfacesAsync(CancellationToken.None).GetAwaiter().GetResult();
                Interfaces = new ObservableCollection<object>(list.Select(i => (object)new AdapterItem(i)));
                OnPropertyChanged(nameof(Interfaces));
            });
        }

        public async Task RequestSwitchDryRunAsync(AdapterItem item)
        {
            try
            {
                var report = await _qualityMonitor.EvaluateAsync(item.ToModel(), CancellationToken.None).ConfigureAwait(false);
                var decision = await _decisionEngine.EvaluateAsync(report, _options, CancellationToken.None).ConfigureAwait(false);
                LastActionMessage = $"Dry-run: would {decision.Action} for {item.Name} (score={report.Score})";
                OnPropertyChanged(nameof(LastActionMessage));
            }
            catch (Exception ex)
            {
                LastActionMessage = "Error during preview: " + ex.Message;
                OnPropertyChanged(nameof(LastActionMessage));
            }
        }

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

        public class AdapterItem
        {
            private readonly NetworkInterfaceInfo _model;
            public string Id => _model.Id;
            public string Name => _model.Name;
            public string Description => _model.Description ?? string.Empty;
            public string Kind => _model.Kind.ToString();
            public string OperationalStatus => _model.OperationalStatus.ToString();
            public string LatestQualityReport { get; private set; } = string.Empty;

            public AdapterItem(NetworkInterfaceInfo model)
            {
                _model = model;
            }

            public NetworkInterfaceInfo ToModel() => _model;

            public void UpdateReport(NetworkQualityReport report)
            {
                LatestQualityReport = $"Latency={report.LatencyMs}ms Loss={report.PacketLossPercent}% Score={report.Score}";
            }
        }
    }
}
