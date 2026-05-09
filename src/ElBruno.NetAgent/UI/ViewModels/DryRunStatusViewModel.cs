using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ElBruno.NetAgent.Core.Models;
using ElBruno.NetAgent.Services.Audit;

namespace ElBruno.NetAgent.UI.ViewModels;

/// <summary>
/// ViewModel for the dry-run status overlay and audit log viewer.
/// </summary>
public class DryRunStatusViewModel : INotifyPropertyChanged
{
    private readonly IAuditLogService _auditLogService;
    private string _statusText = "DRY-RUN MODE";
    private ObservableCollection<AuditLogEntry> _latestEntries = new();
    private int _maxEntries = 20;

    public DryRunStatusViewModel(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
        StatusText = "DRY-RUN MODE";
        RefreshAuditLogAsync();
    }

    /// <summary>
    /// Gets the prominent status text shown in the tray and window.
    /// </summary>
    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText == value) return;
            _statusText = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Latest audit log entries for display.
    /// </summary>
    public ObservableCollection<AuditLogEntry> LatestEntries
    {
        get => _latestEntries;
        private set
        {
            if (_latestEntries == value) return;
            _latestEntries = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Maximum number of entries to display.
    /// </summary>
    public int MaxEntries
    {
        get => _maxEntries;
        set
        {
            if (_maxEntries == value) return;
            _maxEntries = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Command to refresh the audit log entries.
    /// </summary>
    public ICommand RefreshCommand => new RelayCommand(() => RefreshAuditLogAsync());

    private async void RefreshAuditLogAsync()
    {
        try
        {
            var entries = await _auditLogService.GetEntriesAsync();
            var latest = entries
                .OrderByDescending(e => e.Timestamp)
                .Take(MaxEntries)
                .ToList();
            LatestEntries = new ObservableCollection<AuditLogEntry>(latest);
        }
        catch
        {
            // If refresh fails, keep current entries
        }
    }

    public void UpdateStatus(string status)
    {
        StatusText = status;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Simple RelayCommand for WPF binding.
/// </summary>
internal class RelayCommand : ICommand
{
    private readonly Action _execute;

    public RelayCommand(Action execute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _execute();
}
