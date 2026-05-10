using System.Windows;
using ElBruno.NetAgent.Interfaces;

namespace ElBruno.NetAgent.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow(ISettingsViewModel? viewModel = null)
        {
            InitializeComponent();
            if (viewModel != null)
                DataContext = viewModel;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
