using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using VaultDataAPISampleApp.Dialogs;
using VaultDataAPISampleApp.Features.Authentication.Dialogs;
using VaultDataAPISampleApp.Models;
using VaultDataAPISampleApp.Navigation;
using VaultDataAPISampleApp.Platform;
using VaultDataAPISampleApp.Services;
using VaultDataAPISampleApp.State;

namespace VaultDataAPISampleApp.ViewModels
{
    internal sealed class ShellViewModel : ObservableObject
    {
        private static readonly Uri s_clientRegistrationUri =
            new("https://aps.autodesk.com/myapps/");

        private readonly IVaultDataApiService _vaultApiService;
        private readonly VaultSession _session;
        private readonly ISignInDialogService _signInDialogService;
        private readonly IMessageDialogService _messageDialogService;
        private readonly IExternalUriLauncher _externalUriLauncher;
        private readonly IPageNavigationService _navigationService;

        private string _clientId = string.Empty;
        private string _serverAddress = string.Empty;
        private VaultResponse? _selectedVault;
        private NavigationItem? _selectedNavigationItem;

        public ShellViewModel(
            IVaultDataApiService vaultApiService,
            VaultSession session,
            ISignInDialogService signInDialogService,
            IMessageDialogService messageDialogService,
            IExternalUriLauncher externalUriLauncher,
            IPageNavigationService navigationService)
        {
            _vaultApiService = vaultApiService;
            _session = session;
            _signInDialogService = signInDialogService;
            _messageDialogService = messageDialogService;
            _externalUriLauncher = externalUriLauncher;
            _navigationService = navigationService;

            SignInCommand = new AsyncRelayCommand(SignInAsync);
            OpenClientRegistrationCommand = new RelayCommand(OpenClientRegistration);
        }

        public ObservableCollection<VaultResponse> Vaults { get; } = [];

        public IReadOnlyList<NavigationItem> NavigationItems { get; } =
        [
            new(SamplePageId.Files, "File information"),
            new(SamplePageId.FileUpload, "File upload"),
            new(SamplePageId.Users, "User information"),
            new(SamplePageId.ExternalSync, "External Sync")
        ];

        public string ClientId
        {
            get
            {
                return _clientId;
            }
            set
            {
                SetProperty(ref _clientId, value);
            }
        }

        public string ServerAddress
        {
            get
            {
                return _serverAddress;
            }
            set
            {
                SetProperty(ref _serverAddress, value);
            }
        }

        public VaultResponse? SelectedVault
        {
            get
            {
                return _selectedVault;
            }
            set
            {
                if (SetProperty(ref _selectedVault, value))
                {
                    _session.SelectedVault = value;
                }
            }
        }

        public NavigationItem? SelectedNavigationItem
        {
            get
            {
                return _selectedNavigationItem;
            }
            set
            {
                if (SetProperty(ref _selectedNavigationItem, value)
                    && value != null)
                {
                    _navigationService.Navigate(value.PageId);
                }
            }
        }

        public IAsyncRelayCommand SignInCommand { get; }

        public IRelayCommand OpenClientRegistrationCommand { get; }

        public void Initialize()
        {
            SelectedNavigationItem ??= NavigationItems[0];
        }

        private async Task SignInAsync()
        {
            if (string.IsNullOrWhiteSpace(ClientId)
                || string.IsNullOrWhiteSpace(ServerAddress))
            {
                _messageDialogService.ShowInformation(
                    "Please input the client ID and Vault Gateway address");
                return;
            }

            try
            {
                _vaultApiService.ChangeServerAddress(ServerAddress);
                _session.Reset();
                SelectedVault = null;
                Vaults.Clear();

                AuthenticationResult? authenticationResult =
                    _signInDialogService.SignIn(ClientId);
                if (authenticationResult == null)
                {
                    return;
                }

                _vaultApiService.SetAccessToken(authenticationResult.AccessToken);
                _session.SetAuthenticated();
                await LoadVaultsAsync();
            }
            catch (Exception exception)
            {
                _messageDialogService.ShowError(exception.Message);
            }
        }

        private async Task LoadVaultsAsync()
        {
            PaginationResponse<VaultResponse>? result =
                await _vaultApiService.GetVaultsAsync();

            Vaults.Clear();
            if (result?.Results == null)
            {
                return;
            }

            foreach (VaultResponse vault in result.Results)
            {
                Vaults.Add(vault);
            }

            SelectedVault = Vaults.Count > 0 ? Vaults[0] : null;
        }

        private void OpenClientRegistration()
        {
            try
            {
                _externalUriLauncher.Open(s_clientRegistrationUri);
            }
            catch (Exception exception)
            {
                _messageDialogService.ShowError(exception.Message);
            }
        }
    }
}
