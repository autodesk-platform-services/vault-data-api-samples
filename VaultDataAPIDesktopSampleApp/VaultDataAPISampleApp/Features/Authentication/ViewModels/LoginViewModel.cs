using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using VaultDataAPISampleApp.Models;
using VaultDataAPISampleApp.Services;

namespace VaultDataAPISampleApp.Features.Authentication.ViewModels
{
    internal sealed class LoginViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IIdentityService _identityService;
        private readonly string _clientId;

        private CancellationTokenSource? _authenticationCancellationTokenSource;
        private string _status = "Preparing secure sign-in...";
        private bool _isBusy;
        private bool _isAuthenticated;
        private bool _authenticationStarted;

        public LoginViewModel(
            IIdentityService identityService,
            string clientId)
        {
            _identityService = identityService;
            _clientId = clientId;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler? AuthenticationCompleted;

        public string Status
        {
            get
            {
                return _status;
            }
            private set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        public bool IsBusy
        {
            get
            {
                return _isBusy;
            }
            private set
            {
                _isBusy = value;
                OnPropertyChanged();
            }
        }

        public bool IsAuthenticated
        {
            get
            {
                return _isAuthenticated;
            }
            private set
            {
                _isAuthenticated = value;
                OnPropertyChanged();
            }
        }

        public AuthenticationResult? AuthenticationResult { get; private set; }

        public void StartAuthentication()
        {
            if (_authenticationStarted)
            {
                return;
            }

            _authenticationStarted = true;
            _authenticationCancellationTokenSource = new CancellationTokenSource();

            // The WPF content-rendered boundary is synchronous; this task handles and exposes all failures.
            _ = AuthenticateAsync(_authenticationCancellationTokenSource.Token);
        }

        public void Cancel()
        {
            _authenticationCancellationTokenSource?.Cancel();
        }

        public void Dispose()
        {
            _authenticationCancellationTokenSource?.Cancel();
            _authenticationCancellationTokenSource?.Dispose();
        }

        private async Task AuthenticateAsync(CancellationToken cancellationToken)
        {
            IsBusy = true;
            Status = "Waiting for authorization in your browser...";

            try
            {
                AuthenticationResult = await _identityService.AuthenticateAsync(
                    _clientId,
                    cancellationToken);
                IsAuthenticated = true;
                Status = "Sign-in completed.";
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Status = "Sign-in was canceled.";
            }
            catch (Exception exception)
            {
                Status = $"Sign-in failed: {exception.Message}";
            }
            finally
            {
                IsBusy = false;
                AuthenticationCompleted?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
