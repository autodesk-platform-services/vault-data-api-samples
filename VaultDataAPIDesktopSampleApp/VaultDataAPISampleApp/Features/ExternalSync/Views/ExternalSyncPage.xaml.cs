using System.Windows.Controls;

using VaultDataAPISampleApp.Features.ExternalSync.ViewModels;

namespace VaultDataAPISampleApp.Features.ExternalSync.Views
{
    public partial class ExternalSyncPage : Page
    {
        public ExternalSyncPage(ExternalSyncViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel;
        }

        public ExternalSyncViewModel ViewModel { get; }
    }
}
