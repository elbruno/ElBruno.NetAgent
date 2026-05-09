using System;
using System.Windows;
using ElBruno.NetAgent.Interfaces;

namespace ElBruno.NetAgent.Views
{
    public partial class StatusWindow : Window
    {
        public StatusWindow(IStatusViewModel? viewModel = null)
        {
            InitializeComponent();
            if (viewModel != null)
                DataContext = viewModel;
        }
    }
}
