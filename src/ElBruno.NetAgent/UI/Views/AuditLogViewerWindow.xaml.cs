using System.Windows;
using ElBruno.NetAgent.Services.Audit;
using ElBruno.NetAgent.UI.ViewModels;

namespace ElBruno.NetAgent.UI.Views;

/// <summary>
/// Interaction logic for AuditLogViewerWindow.xaml
/// </summary>
public partial class AuditLogViewerWindow : Window
{
    public DryRunStatusViewModel ViewModel { get; }

    public AuditLogViewerWindow(IAuditLogService auditLogService)
    {
        InitializeComponent();
        ViewModel = new DryRunStatusViewModel(auditLogService);
        DataContext = ViewModel;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
