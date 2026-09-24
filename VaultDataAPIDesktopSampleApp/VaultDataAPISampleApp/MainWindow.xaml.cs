using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using VaultDataAPISampleApp.Models;
using VaultDataAPISampleApp.Services;
using VaultDataAPISampleApp.ViewModels;

namespace VaultDataAPISampleApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [SupportedOSPlatform("windows10.0.19041.0")]
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<UserResponse> _userData = [];
        private readonly ObservableCollection<FileVersionResponse> _fileData = [];

        private readonly VaultAPIService _vaultAPIService;
        private readonly IIdentityService _identityService;
        private readonly ExternalSyncTabControl _externalSyncTabControl;

        private string _userCount = string.Empty;
        private string _fileCount = string.Empty;
        private List<VaultResponse> _vaultList = [];

        public MainWindow(
            VaultAPIService vaultAPIService,
            IIdentityService identityService,
            TabViewModel tabViewModel,
            ExternalSyncTabControl externalSyncTabControl)
        {
            InitializeComponent();
            _vaultAPIService = vaultAPIService;
            _identityService = identityService;
            _externalSyncTabControl = externalSyncTabControl;
            DataContext = tabViewModel;
            ExternalSyncContent.Content = externalSyncTabControl;
            externalSyncTabControl.DataContext = null;

            UserListView.ItemsSource = _userData;
            FileListView.ItemsSource = _fileData;
        }

        private void SetFileDataBackgroudVisibility(bool isVisible)
        {
            DataBackgroundImage.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            DataBackgroundText.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;

            ChartBackgroundImage.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            ChartBackgroundText.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetUserListViewBackgroudVisibility(bool isVisible)
        {
            UserDataBackgroundImage.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            UserDataBackgroundText.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private bool VerifyAuthenticated()
        {
            if (!_vaultAPIService.HasAccessToken)
            {
                MessageBox.Show("Please login first");
                return false;
            }

            return true;
        }

        private async void GetUser_Click(object sender, RoutedEventArgs e)
        {
            if (VerifyAuthenticated())
            {
                await RunUiOperationAsync(GetUserDataAsync);
            }
        }

        private async void GetFile_Click(object sender, RoutedEventArgs e)
        {
            if (VerifyAuthenticated())
            {
                if (GetSelectedVaultId() == null)
                {
                    MessageBox.Show("Please select a vault first");
                    return;
                }

                await RunUiOperationAsync(GetFileDataAsync);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            var psi = new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            };
            Process.Start(psi);
            e.Handled = true;
        }

        private async Task GetUserDataAsync()
        {
            PaginationResponse<UserResponse>? result = await _vaultAPIService.GetUsersAsync();
            if (result?.Pagination == null || result.Results == null)
            {
                return;
            }

            _userCount = result.Pagination.TotalResults.ToString();
            UserCount.Content = _userCount;
            _userData.Clear();

            foreach (UserResponse user in result.Results)
            {
                _userData.Add(user);
            }

            SetUserListViewBackgroudVisibility(_userData.Count == 0);
        }

        private async Task GetFileDataAsync()
        {
            string? vaultId = GetSelectedVaultId();
            if (vaultId == null)
            {
                return;
            }

            PaginationResponse<FileVersionResponse>? result =
                await _vaultAPIService.GetFilesAsync(vaultId);
            if (!string.Equals(GetSelectedVaultId(), vaultId, StringComparison.Ordinal))
            {
                return;
            }

            if (result?.Pagination == null || result.Results == null)
            {
                return;
            }

            _fileCount = result.Pagination.TotalResults.ToString();
            FileCount.Content = _fileCount;
            _fileData.Clear();

            foreach (FileVersionResponse file in result.Results)
            {
                _fileData.Add(file);
            }

            AnalyzeFileType();
            SetFileDataBackgroudVisibility(_fileData.Count == 0);

        }

        private void AnalyzeFileType()
        {
            var iptFileCount = _fileData.Count(file => file.Name?.Contains(".ipt", StringComparison.OrdinalIgnoreCase) == true);
            var iamFileCount = _fileData.Count(file => file.Name?.Contains(".iam", StringComparison.OrdinalIgnoreCase) == true);
            var dwgFileCount = _fileData.Count(file => file.Name?.Contains(".dwg", StringComparison.OrdinalIgnoreCase) == true);
            var dwfFileCount = _fileData.Count(file => file.Name?.Contains(".dwf", StringComparison.OrdinalIgnoreCase) == true);
            var otherFileCount = _fileData.Count - (iptFileCount + iamFileCount + dwgFileCount + dwfFileCount);

            ISeries[] series =
            [
                CreateFileTypeSeries("Ipt file", iptFileCount, new SKColor(0xE0, 0xAF, 0x4B)),
                CreateFileTypeSeries("Iam file", iamFileCount, new SKColor(0xE1, 0xE1, 0x54)),
                CreateFileTypeSeries("Dwg file", dwgFileCount, new SKColor(0x68, 0x9E, 0xD4)),
                CreateFileTypeSeries("Dwf file", dwfFileCount, new SKColor(0x9C, 0x6B, 0xCE)),
                CreateFileTypeSeries("Other file", otherFileCount, new SKColor(0xB2, 0xB2, 0xB5))
            ];
            PieChart.Series = series;
        }

        private static PieSeries<double> CreateFileTypeSeries(
            string name,
            double value,
            SKColor color)
        {
            var series = new PieSeries<double>
            {
                Name = name,
                Values = [value],
                Fill = new SolidColorPaint(color),
                DataLabelsPaint = new SolidColorPaint(SKColors.Black)
            };
            return series;
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ClientID.Text)
                || string.IsNullOrWhiteSpace(BaseUrl.Text))
            {
                MessageBox.Show("Please input the client ID and Vault Gateway address");
                return;
            }

            _vaultAPIService.ChangeServerAddress(BaseUrl.Text);

            var loginWindow = new ADSKLoginWindow(ClientID.Text, _identityService)
            {
                Owner = this
            };
            if (loginWindow.ShowDialog() == true
                && loginWindow.AuthenticationResult is AuthenticationResult authenticationResult)
            {
                _vaultAPIService.SetAccessToken(authenticationResult.AccessToken);
                await RunUiOperationAsync(GetVaultsAsync);
            }
        }

        private async Task GetVaultsAsync()
        {
            _vaultList = [];
            VaultList.Items.Clear();
            _externalSyncTabControl.SelectedVaultId = null;

            PaginationResponse<VaultResponse>? result = await _vaultAPIService.GetVaultsAsync();
            List<VaultResponse>? vaults = result?.Results;
            if (vaults?.Count > 0)
            {
                _vaultList = vaults;
                foreach (VaultResponse vault in vaults)
                {
                    VaultList.Items.Add(vault.Name);
                }

                VaultList.SelectedIndex = 0;
            }
        }

        private void Vault_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ResetVaultFileState();

            if (VaultList.SelectedIndex >= 0 && VaultList.SelectedIndex < _vaultList.Count)
            {
                _externalSyncTabControl.SelectedVaultId =
                    _vaultList[VaultList.SelectedIndex].Id;
                return;
            }

            _externalSyncTabControl.SelectedVaultId = null;
        }

        private void ResetVaultFileState()
        {
            _fileData.Clear();
            _fileCount = string.Empty;
            FileCount.Content = _fileCount;
            AnalyzeFileType();
            SetFileDataBackgroudVisibility(true);
        }

        private string? GetSelectedVaultId()
        {
            if (VaultList.SelectedIndex < 0 || VaultList.SelectedIndex >= _vaultList.Count)
            {
                return null;
            }

            string? vaultId = _vaultList[VaultList.SelectedIndex].Id;
            return string.IsNullOrWhiteSpace(vaultId) ? null : vaultId;
        }

        private static async Task RunUiOperationAsync(Func<Task> operation)
        {
            try
            {
                await operation();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Vault Data API", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
