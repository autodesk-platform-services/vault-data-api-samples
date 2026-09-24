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
            DataContext = tabViewModel;
            ExternalSyncContent.Content = externalSyncTabControl;

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

        private bool VerifyTokenAndVaultIsReady()
        {
            string? clientId = _vaultAPIService.GetClientId();
            string? token = _vaultAPIService.GetAccessToken();
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(token))
            {
                MessageBox.Show("Please login first");
                return false;
            }

            return true;
        }

        private void GetUser_Click(object sender, RoutedEventArgs e)
        {
            if (VerifyTokenAndVaultIsReady())
            {
                GetUserData();
            }
        }

        private void GetFile_Click(object sender, RoutedEventArgs e)
        {
            if (VerifyTokenAndVaultIsReady())
            {
                GetFileData();
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

        private void GetUserData()
        {
            _ = _vaultAPIService.GetUsersAsync().ContinueWith((task) =>
            {
                PaginationResponse<UserResponse>? result =
                    task.Status == System.Threading.Tasks.TaskStatus.RanToCompletion
                        ? task.Result
                        : null;
                if (result?.Pagination != null && result.Results != null)
                {
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        _userCount = result.Pagination.TotalResults.ToString();
                        UserCount.Content = _userCount;

                        // Clear the old data
                        _userData.Clear();

                        foreach (UserResponse user in result.Results)
                        {
                            _userData.Add(user);
                        }

                        if (_userData.Count > 0)
                        {
                            SetUserListViewBackgroudVisibility(false);
                        }
                        else
                        {
                            SetUserListViewBackgroudVisibility(true);
                        }
                    }));
                }
            });
        }

        private void GetFileData()
        {
            _ = _vaultAPIService.GetFilesAsync().ContinueWith((task) =>
            {
                PaginationResponse<FileVersionResponse>? result =
                    task.Status == System.Threading.Tasks.TaskStatus.RanToCompletion
                        ? task.Result
                        : null;
                if (result?.Pagination != null && result.Results != null)
                {
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        _fileCount = result.Pagination.TotalResults.ToString();
                        FileCount.Content = _fileCount;

                        // Clear the old data
                        _fileData.Clear();

                        foreach (FileVersionResponse file in result.Results)
                        {
                            _fileData.Add(file);
                        }
                        // Analyze the file type
                        AnalyzeFileType();

                        if (_fileData.Count > 0)
                        {
                            SetFileDataBackgroudVisibility(false);
                        }
                        else
                        {
                            SetFileDataBackgroudVisibility(true);
                        }
                    }));
                }
            });

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

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ClientID.Text)
                || string.IsNullOrWhiteSpace(BaseUrl.Text))
            {
                MessageBox.Show("Please input the client ID and Vault Gateway address");
                return;
            }

            _vaultAPIService.SetServerAddress(BaseUrl.Text);
            _vaultAPIService.SetClientId(ClientID.Text);

            var loginWindow = new ADSKLoginWindow(ClientID.Text, _identityService)
            {
                Owner = this
            };
            if (loginWindow.ShowDialog() == true
                && loginWindow.AuthenticationResult is AuthenticationResult authenticationResult)
            {
                _vaultAPIService.SetAccessToken(authenticationResult.AccessToken);
                GetVaults();
            }
        }

        private void GetVaults()
        {
            _ = _vaultAPIService.GetVaultsAsync().ContinueWith((task) =>
            {
                List<VaultResponse>? vaults =
                    task.Status == System.Threading.Tasks.TaskStatus.RanToCompletion
                        ? task.Result?.Results
                        : null;
                if (task.Status != System.Threading.Tasks.TaskStatus.Faulted && vaults?.Count > 0)
                {
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        _vaultList = vaults;
                        foreach (VaultResponse vaultServer in vaults)
                        {
                            VaultList.Items.Add(vaultServer.Name);
                        }
                        VaultList.SelectedIndex = 0;
                        _vaultAPIService.SetVaultServer(_vaultList[0]);
                    }));
                }
                else if (task.Exception != null && task.Exception.InnerException != null)
                {
                    // remove all item in the list
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        VaultList.Items.Clear();
                        MessageBox.Show(task.Exception.GetBaseException().Message);
                    }));
                }
            });
        }

        private void Vault_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VaultList.SelectedIndex >= 0 && VaultList.SelectedIndex < _vaultList.Count)
            {
                _vaultAPIService.SetVaultServer(_vaultList[VaultList.SelectedIndex]);
            }
        }

    }
}
