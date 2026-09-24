using CommunityToolkit.Mvvm.ComponentModel;

using VaultDataAPISampleApp.Models;

namespace VaultDataAPISampleApp.State
{
    public sealed class VaultSession : ObservableObject
    {
        private bool _isAuthenticated;
        private VaultResponse? _selectedVault;

        public bool IsAuthenticated
        {
            get
            {
                return _isAuthenticated;
            }
            private set
            {
                SetProperty(ref _isAuthenticated, value);
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
                SetProperty(ref _selectedVault, value);
            }
        }

        public string? SelectedVaultId => SelectedVault?.Id;

        public void SetAuthenticated()
        {
            IsAuthenticated = true;
        }

        public void Reset()
        {
            SelectedVault = null;
            IsAuthenticated = false;
        }

        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.PropertyName == nameof(SelectedVault))
            {
                OnPropertyChanged(nameof(SelectedVaultId));
            }
        }
    }
}
