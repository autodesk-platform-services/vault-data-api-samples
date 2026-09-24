using System.Windows.Controls;

using VaultDataAPISampleApp.Features.FileUpload.ViewModels;

namespace VaultDataAPISampleApp.Features.FileUpload.Views
{
    public partial class FileUploadPage : Page
    {
        public FileUploadPage(FileUploadViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
