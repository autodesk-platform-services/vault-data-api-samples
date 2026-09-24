using System.Windows;

using VaultDataAPISampleApp.Features.Authentication.ViewModels;
using VaultDataAPISampleApp.Features.Authentication.Views;
using VaultDataAPISampleApp.Models;

namespace VaultDataAPISampleApp.Features.Authentication.Dialogs
{
    internal sealed class WpfSignInDialogService : ISignInDialogService
    {
        private readonly LoginViewModelFactory _loginViewModelFactory;

        public WpfSignInDialogService(
            LoginViewModelFactory loginViewModelFactory)
        {
            _loginViewModelFactory = loginViewModelFactory;
        }

        public AuthenticationResult? SignIn(string clientId)
        {
            using LoginViewModel viewModel =
                _loginViewModelFactory.Create(clientId);
            var loginWindow = new ADSKLoginWindow(viewModel)
            {
                Owner = Application.Current.MainWindow
            };

            return loginWindow.ShowDialog() == true
                ? viewModel.AuthenticationResult
                : null;
        }
    }
}
