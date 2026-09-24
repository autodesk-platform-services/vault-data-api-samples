using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using VaultDataAPISampleApp.Dialogs;
using VaultDataAPISampleApp.Models;
using VaultDataAPISampleApp.Services;
using VaultDataAPISampleApp.State;

namespace VaultDataAPISampleApp.Features.Users.ViewModels
{
    public sealed class UsersViewModel : ObservableObject, IDisposable
    {
        private readonly IVaultDataApiService _vaultApiService;
        private readonly VaultSession _session;
        private readonly IMessageDialogService _messageDialogService;

        private int _totalCount;

        public UsersViewModel(
            IVaultDataApiService vaultApiService,
            VaultSession session,
            IMessageDialogService messageDialogService)
        {
            _vaultApiService = vaultApiService;
            _session = session;
            _messageDialogService = messageDialogService;

            LoadUsersCommand = new AsyncRelayCommand(LoadUsersAsync, CanLoadUsers);
            _session.PropertyChanged += SessionPropertyChanged;
        }

        public ObservableCollection<UserResponse> Users { get; } = [];

        public int TotalCount
        {
            get
            {
                return _totalCount;
            }
            private set
            {
                SetProperty(ref _totalCount, value);
            }
        }

        public bool IsEmpty => Users.Count == 0;

        public IAsyncRelayCommand LoadUsersCommand { get; }

        public void Dispose()
        {
            _session.PropertyChanged -= SessionPropertyChanged;
        }

        private bool CanLoadUsers()
        {
            return _session.IsAuthenticated;
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                PaginationResponse<UserResponse>? result =
                    await _vaultApiService.GetUsersAsync();
                if (result?.Pagination == null || result.Results == null)
                {
                    return;
                }

                Users.Clear();
                foreach (UserResponse user in result.Results)
                {
                    Users.Add(user);
                }

                TotalCount = result.Pagination.TotalResults;
                OnPropertyChanged(nameof(IsEmpty));
            }
            catch (Exception exception)
            {
                _messageDialogService.ShowError(exception.Message);
            }
        }

        private void SessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VaultSession.IsAuthenticated))
            {
                LoadUsersCommand.NotifyCanExecuteChanged();
            }
        }
    }
}
