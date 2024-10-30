using System;
using System.Collections.Generic;
using LiveCharts.Wpf;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using VaultDataAPISampleApp.Models;
using VaultDataAPISampleApp.ViewModels;

namespace VaultDataAPISampleApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<UserResponse> _userData = new ObservableCollection<UserResponse>();
        private ObservableCollection<FileVersionResponse> _fileData = new ObservableCollection<FileVersionResponse>();

        private string _userCount;
        private string _fileCount;
        private List<VaultResponse> _vaultList;

        public MainWindow()
        {
            InitializeComponent();
            this.Closing += MainWindow_Closing;
            this.DataContext = new TabViewModel();

            UserListView.Items.Clear();
            UserListView.ItemsSource = _userData;
            FileListView.Items.Clear();
            FileListView.ItemsSource = _fileData;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
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
            try
            {
                string clientId = VaultAPIService.Instance.GetClientId();
                string token = VaultAPIService.Instance.GetAccessToken();
                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(token))
                {
                    MessageBox.Show("Please login first");
                    return false;
                }
                return true;
            }
            catch (Exception)
            {
                MessageBox.Show("Please login first");
                return false;
            }

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
            _ = VaultAPIService.Instance.GetUsers().ContinueWith((task) =>
            {
                if (task.Result != null)
                {
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        _userCount = task.Result.Pagination.TotalResults.ToString();
                        UserCount.Content = _userCount;

                        // Clear the old data
                        _userData.Clear();

                        foreach (var user in task.Result.Results)
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
            _ = VaultAPIService.Instance.GetFiles().ContinueWith((task) =>
            {
                if (task.Result != null)
                {
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        _fileCount = task.Result.Pagination.TotalResults.ToString();
                        FileCount.Content = _fileCount;

                        // Clear the old data
                        _fileData.Clear();

                        foreach (var file in task.Result.Results)
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
            double iptFileCount = 0;
            double iamFileCount = 0;
            double dwgFileCount = 0;
            double dwfFileCount = 0;
            double otherFileCount = 0;

            // ipt file count
            PieSeries iptSeries = PieChart.Series.FirstOrDefault(s => ((PieSeries)s).Title == "Ipt file") as PieSeries;

            if (iptSeries != null)
            {
                // Calculate the file type in _fileData
                iptFileCount = _fileData.Count(f => f.Name.ToLowerInvariant().Contains(".ipt"));
                // Clear the old values and add the new value.
                iptSeries.Values.Clear();
                iptSeries.Values.Add(iptFileCount);
                iptSeries.LabelPoint = point => point.Y.ToString();
                iptSeries.DataLabels = true;
            }

            // iam file count
            PieSeries iamSeries = PieChart.Series.FirstOrDefault(s => ((PieSeries)s).Title == "Iam file") as PieSeries;

            if (iamSeries != null)
            {
                // Calculate the file type in _fileData
                iamFileCount = _fileData.Count(f => f.Name.ToLowerInvariant().Contains(".iam"));
                // Clear the old values and add the new value.
                iamSeries.Values.Clear();
                iamSeries.Values.Add(iamFileCount);
                iamSeries.LabelPoint = point => point.Y.ToString();
                iamSeries.DataLabels = true;
            }

            // dwg file count
            PieSeries dwgSeries = PieChart.Series.FirstOrDefault(s => ((PieSeries)s).Title == "Dwg file") as PieSeries;

            if (dwgSeries != null)
            {
                // Calculate the file type in _fileData
                dwgFileCount = _fileData.Count(f => f.Name.ToLowerInvariant().Contains(".dwg"));
                // Clear the old values and add the new value.
                dwgSeries.Values.Clear();
                dwgSeries.Values.Add(dwgFileCount);
                dwgSeries.LabelPoint = point => point.Y.ToString();
                dwgSeries.DataLabels = true;
            }

            // dwf file count
            PieSeries dwfSeries = PieChart.Series.FirstOrDefault(s => ((PieSeries)s).Title == "Dwf file") as PieSeries;

            if (dwfSeries != null)
            {
                // Calculate the file type in _fileData
                dwfFileCount = _fileData.Count(f => f.Name.ToLowerInvariant().Contains(".dwf"));
                // Clear the old values and add the new value.
                dwfSeries.Values.Clear();
                dwfSeries.Values.Add(dwfFileCount);
                dwfSeries.LabelPoint = point => point.Y.ToString();
                dwfSeries.DataLabels = true;
            }

            // other file count
            PieSeries otherSeries = PieChart.Series.FirstOrDefault(s => ((PieSeries)s).Title == "Other file") as PieSeries;
            if (otherSeries != null)
            {
                otherFileCount = _fileData.Count - (iptFileCount + iamFileCount + dwgFileCount + dwfFileCount);
                otherSeries.Values.Clear();
                otherSeries.Values.Add(otherFileCount);
                otherSeries.LabelPoint = point => point.Y.ToString();
                otherSeries.DataLabels = true;
            }
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ClientID.Text) && string.IsNullOrEmpty(BaseUrl.Text))
            {
                MessageBox.Show("Please input the client ID and Vault Gateway address");
                return;
            }

            if (VaultAPIService.Instance == null)
            {
                VaultAPIService.Initialize(BaseUrl.Text);
            }
            else
            {
                if (VaultAPIService.Instance.GetServerAddress() != BaseUrl.Text)
                {
                    VaultAPIService.ResetInstance();
                    VaultAPIService.Initialize(BaseUrl.Text);
                }
            }

            VaultAPIService.Instance.SetClientId(ClientID.Text);

            var loginWindow = new ADSKLoginWindow(ClientID.Text);
            if (loginWindow.ShowDialog() == true)
            {
                GetVaults();
            }   
        }

        private void GetVaults()
        {
            _ = VaultAPIService.Instance.GetVaults().ContinueWith((task) =>
            {
                if ((task.Status != System.Threading.Tasks.TaskStatus.Faulted) && (task.Result != null))
                {
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        _vaultList = task.Result.Results;
                        foreach (var vaultServer in task.Result.Results)
                        {
                            VaultList.Items.Add(vaultServer.Name);
                        }
                        VaultList.SelectedIndex = 0;
                        VaultAPIService.Instance.SetVaultServer(_vaultList[0]);
                    }));
                }
                else if (task.Exception != null && task.Exception.InnerException != null)
                {
                    // remove all item in the list
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        VaultList.Items.Clear();
                        MessageBox.Show(task.Exception.InnerException.InnerException.Message);
                    }));
                }
            });
        }

        private void Vault_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VaultList.SelectedIndex >= 0 && VaultList.SelectedIndex < _vaultList.Count)
            {
                VaultAPIService.Instance.SetVaultServer(_vaultList[VaultList.SelectedIndex]);
            }
        }

    }
}