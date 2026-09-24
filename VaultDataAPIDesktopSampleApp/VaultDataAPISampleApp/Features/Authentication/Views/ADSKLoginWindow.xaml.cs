using System;
using System.Runtime.Versioning;
using System.Windows;

using VaultDataAPISampleApp.Features.Authentication.ViewModels;

namespace VaultDataAPISampleApp.Features.Authentication.Views
{
    /// <summary>
    /// Interaction logic for ADSKLoginWindow.xaml
    /// </summary>
    [SupportedOSPlatform("windows10.0.19041.0")]
    public partial class ADSKLoginWindow : Window
    {
        private readonly LoginViewModel _viewModel;

        private bool _authenticationStarted;

        internal ADSKLoginWindow(LoginViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _viewModel.AuthenticationCompleted += AuthenticationCompleted;
            DataContext = _viewModel;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            if (_authenticationStarted)
            {
                return;
            }

            _authenticationStarted = true;
            _viewModel.StartAuthentication();
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel.AuthenticationCompleted -= AuthenticationCompleted;
            base.OnClosed(e);
        }

        private void AuthenticationCompleted(object? sender, EventArgs e)
        {
            if (_viewModel.IsAuthenticated)
            {
                DialogResult = true;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Cancel();
            DialogResult = false;
        }
    }
}
