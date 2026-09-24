using System.Windows.Controls;

using VaultDataAPISampleApp.Features.Users.ViewModels;

namespace VaultDataAPISampleApp.Features.Users.Views
{
    public partial class UsersPage : Page
    {
        public UsersPage(UsersViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel;
        }

        public UsersViewModel ViewModel { get; }
    }
}
