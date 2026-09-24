using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;

using VaultDataAPISampleApp.ViewModels;

namespace VaultDataAPISampleApp
{
    [SupportedOSPlatform("windows10.0.19041.0")]
    public partial class MainWindow : Window
    {
        internal MainWindow(ShellViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        internal Frame NavigationFrame => PageFrame;
    }
}
