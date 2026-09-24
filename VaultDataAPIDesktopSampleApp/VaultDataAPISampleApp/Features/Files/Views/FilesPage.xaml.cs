using System.Windows.Controls;

using VaultDataAPISampleApp.Features.Files.ViewModels;

namespace VaultDataAPISampleApp.Features.Files.Views
{
    public partial class FilesPage : Page
    {
        public FilesPage(FilesViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel;
        }

        public FilesViewModel ViewModel { get; }
    }
}
