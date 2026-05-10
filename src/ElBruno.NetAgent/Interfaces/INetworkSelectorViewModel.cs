using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ElBruno.NetAgent.Interfaces
{
    public interface INetworkSelectorViewModel : INotifyPropertyChanged
    {
        ObservableCollection<object> Interfaces { get; }
        string LastActionMessage { get; }
        ICommand RefreshCommand { get; }
        ICommand RequestSwitchCommand { get; }
        Task RefreshAsync();
    }
}