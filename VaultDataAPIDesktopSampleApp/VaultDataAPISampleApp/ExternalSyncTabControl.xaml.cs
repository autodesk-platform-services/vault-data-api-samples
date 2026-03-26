using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VaultDataAPISampleApp.Models;

namespace VaultDataAPISampleApp
{
    /// <summary>
    /// Interaction logic for ExternalSyncTabControl.xaml
    /// </summary>
    public partial class ExternalSyncTabControl : UserControl
    {
        private const string DefaultExtSyncConfigId = "Adsk.Vault.ExternalSyncTask.FusionManage";
        private const string DefaultExtSyncWorkflowType = "Adsk.UploadItem";
        private const string FusionManageDetailsInfoName = "Adsk.FusionManage.Details";
        private const string FusionManageStatusInfoName = "Adsk.FusionManage.Status";

        private enum WorkResultStatus
        {
            Unknown = 0,
            Succeeded = 1,
            Failed = 2,
            PartiallySucceeded = 3,
            Skipped = 4
        }

        private readonly ObservableCollection<ItemVersionResponse> _itemsData = new ObservableCollection<ItemVersionResponse>();
        private bool _isRefreshingSyncInfo;
        private CancellationTokenSource _syncInfoCts;

        public ExternalSyncTabControl()
        {
            InitializeComponent();
            ItemsGrid.ItemsSource = _itemsData;
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

        private async void LoadConfigs_Click(object sender, RoutedEventArgs e)
        {
            if (!VerifyTokenAndVaultIsReady()) return;

            ExtSyncConfigsText.Text = "Loading configs...";
            var configs = await VaultAPIService.Instance.GetExtSyncConfigsAsync();

            if (configs != null && configs.Count > 0)
            {
                ExtSyncConfigsText.Text = string.Join("  |  ", configs.Select(kv => $"{kv.Key}: {kv.Value}"));
                ExtSyncHintText.Text = "Configs loaded successfully. You can now operate on items and tasks.";
                ExtSyncHintText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#107C10"));
                LoadItemsPanel.IsEnabled = true;
                ExtSyncContentPanel.IsEnabled = true;
                ExtSyncContentPanel.Opacity = 1.0;
                SetStatus($"Loaded {configs.Count} config(s). Operations enabled.");
            }
            else
            {
                ExtSyncConfigsText.Text = configs == null ? "(failed to load)" : "(no configs found)";
                ExtSyncHintText.Text = "No external sync configs found. Please configure mapping in Vault Client first.";
                ExtSyncHintText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D13438"));
                LoadItemsPanel.IsEnabled = false;
                ExtSyncContentPanel.IsEnabled = false;
                ExtSyncContentPanel.Opacity = 0.5;
                SetStatus("No configs found. Operations disabled.");
            }
        }

        private async void ListTasks_Click(object sender, RoutedEventArgs e)
        {
            if (!VerifyTokenAndVaultIsReady()) return;

            int limit = int.TryParse(TaskListLimitInput.Text, out var parsed) ? parsed : 50;
            SyncTasksGroupBox.Header = "Sync Tasks - All";
            SyncTasksEmptyText.Text = "Loading tasks...";
            SyncTasksEmptyText.Visibility = Visibility.Visible;
            SyncTasksGrid.ItemsSource = null;

            var result = await VaultAPIService.Instance.GetExtSyncTasksAsync(limit: limit);
            if (result == null) return;

            SyncTasksGrid.ItemsSource = result.Results;
            var count = result.Results?.Count ?? 0;
            SyncTasksEmptyText.Visibility = count > 0 ? Visibility.Collapsed : Visibility.Visible;
            if (count == 0) SyncTasksEmptyText.Text = "No tasks found.";
            SetStatus($"Listed {count} task(s) (limit={limit}).");
        }

        private async void LoadItems_Click(object sender, RoutedEventArgs e)
        {
            if (!VerifyTokenAndVaultIsReady()) return;

            int limit = int.TryParse(ItemListLimitInput.Text, out var parsed) ? parsed : 50;
            ItemsEmptyText.Text = "Loading items...";
            ItemsEmptyText.Visibility = Visibility.Visible;

            var result = await VaultAPIService.Instance.GetItemVersionsAsync(limit: limit);
            if (result == null) return;

            _itemsData.Clear();
            foreach (var item in result.Results ?? new List<ItemVersionResponse>())
            {
                _itemsData.Add(item);
            }

            ItemsEmptyText.Visibility = _itemsData.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            if (_itemsData.Count == 0) ItemsEmptyText.Text = "No items found.";
            SetStatus($"Loaded {_itemsData.Count} items.");
        }

        private async void ItemsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = ItemsGrid.SelectedItem as ItemVersionResponse;
            if (item == null)
            {
                RefreshSyncInfoButton.IsEnabled = false;
                ClearSyncInfoCards();
                SyncInfoEmptyText.Text = "Select an item to view sync info.";
                SyncInfoEmptyText.Visibility = Visibility.Visible;
                ExtSyncSelectedItemText.Text = string.Empty;
                return;
            }

            if (!VerifyTokenAndVaultIsReady()) return;

            var hasMasterId = !string.IsNullOrWhiteSpace(item.Item?.Id);
            RefreshSyncInfoButton.IsEnabled = !_isRefreshingSyncInfo && hasMasterId;
            ExtSyncSelectedItemText.Text = $"Selected: {item.Number} (Id: {item.Id})";
            await RefreshSyncInfoForItemAsync(item, $"Loading sync info for '{item.Number}'...");
        }

        private async System.Threading.Tasks.Task RefreshSyncInfoForItemAsync(ItemVersionResponse item, string loadingStatusMessage)
        {
            if (item == null) return;

            _syncInfoCts?.Cancel();
            _syncInfoCts = new CancellationTokenSource();
            var cts = _syncInfoCts;

            SetSyncInfoRefreshBusy(true);
            ClearSyncInfoCards();
            SyncInfoEmptyText.Text = "Loading sync info...";
            SyncInfoEmptyText.Visibility = Visibility.Visible;

            try
            {
                if (!string.IsNullOrWhiteSpace(loadingStatusMessage))
                {
                    SetStatus(loadingStatusMessage);
                }

                var itemMasterId = item.Item?.Id;
                if (string.IsNullOrWhiteSpace(itemMasterId))
                {
                    if (cts.IsCancellationRequested) return;
                    ClearSyncInfoCards();
                    SyncInfoEmptyText.Text = $"Item '{item.Number}' has no master id.";
                    SyncInfoEmptyText.Visibility = Visibility.Visible;
                    SetStatus($"Item '{item.Number}' does not include a master id.");
                    return;
                }

                var syncInfoResult = await VaultAPIService.Instance.GetItemExtSyncInfosAsync(itemMasterId);
                if (cts.IsCancellationRequested) return;

                var infos = syncInfoResult?.Results;
                if (infos != null && infos.Count > 0)
                {
                    var formattedInfos = FormatExtSyncInfosForDisplay(infos);
                    ShowSyncInfoCards(formattedInfos);
                    SyncInfoEmptyText.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ClearSyncInfoCards();
                    SyncInfoEmptyText.Text = $"No sync info found for '{item.Number}'.";
                    SyncInfoEmptyText.Visibility = Visibility.Visible;
                }

                SetStatus($"Item '{item.Number}': {infos?.Count ?? 0} sync info(s).");
            }
            finally
            {
                if (!cts.IsCancellationRequested)
                    SetSyncInfoRefreshBusy(false);
            }
        }

        private async void RefreshSyncInfoButton_Click(object sender, RoutedEventArgs e)
        {
            var item = ItemsGrid.SelectedItem as ItemVersionResponse;
            if (item == null)
            {
                SetStatus("Select an item first, then refresh sync info.");
                return;
            }

            if (!VerifyTokenAndVaultIsReady()) return;
            await RefreshSyncInfoForItemAsync(item, $"Refreshing sync info for '{item.Number}'...");
        }

        private async void CreateTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var item = button?.DataContext as ItemVersionResponse;
            if (item == null) return;
            if (!VerifyTokenAndVaultIsReady()) return;

            var dialog = new CreateTaskConfirmWindow(DefaultExtSyncConfigId, DefaultExtSyncWorkflowType);
            var ownerWindow = Window.GetWindow(this);
            if (ownerWindow != null)
            {
                dialog.Owner = ownerWindow;
            }

            if (dialog.ShowDialog() != true) return;

            var configId = string.IsNullOrWhiteSpace(dialog.ConfigId)
                ? DefaultExtSyncConfigId
                : dialog.ConfigId.Trim();
            var workflowType = string.IsNullOrWhiteSpace(dialog.WorkflowType)
                ? DefaultExtSyncWorkflowType
                : dialog.WorkflowType.Trim();

            var selectedItems = ItemsGrid.SelectedItems.Cast<ItemVersionResponse>().ToList();
            if (selectedItems.Count > 1 && selectedItems.Contains(item))
            {
                var requests = selectedItems.Select(i => new CreateExtSyncTaskRequest
                {
                    EntityId = i.Id,
                    EntityClassId = "ITEM",
                    ConfigId = configId,
                    WorkflowType = workflowType,
                    Description = $"Sync to Fusion Manage ({i.Id})",
                    SendAgentEvent = true
                }).ToList();

                var tasks = await VaultAPIService.Instance.BatchCreateExtSyncTasksAsync(requests);
                if (tasks == null) return;

                if (tasks.Count == 0)
                {
                    SyncTasksGrid.ItemsSource = null;
                    SyncTasksEmptyText.Text = "No external sync tasks were created.";
                    SyncTasksEmptyText.Visibility = Visibility.Visible;
                    SetStatus("No external sync tasks were created.");
                }
                else
                {
                    SyncTasksGrid.ItemsSource = tasks;
                    SyncTasksEmptyText.Visibility = Visibility.Collapsed;
                    SetStatus($"Batch created {tasks.Count} task(s) for {selectedItems.Count} item(s).");
                }
            }
            else
            {
                var request = new CreateExtSyncTaskRequest
                {
                    EntityId = item.Id,
                    EntityClassId = "ITEM",
                    ConfigId = configId,
                    WorkflowType = workflowType,
                    Description = $"Sync to Fusion Manage ({item.Id})",
                    SendAgentEvent = true
                };

                var task = await VaultAPIService.Instance.CreateExtSyncTaskAsync(request);
                if (task == null) return;

                SyncTasksGrid.ItemsSource = new List<ExtSyncTaskResponse> { task };
                SyncTasksEmptyText.Visibility = Visibility.Collapsed;
                SetStatus($"Created task  Id: {task.Id}  Status: {task.Status}");
            }
        }

        private async void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var taskItem = button?.DataContext as ExtSyncTaskResponse;
            if (taskItem == null) return;
            if (!VerifyTokenAndVaultIsReady()) return;

            var confirm = MessageBox.Show($"Are you sure you want to delete task '{taskItem.Id}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            var success = await VaultAPIService.Instance.DeleteExtSyncTaskAsync(taskItem.Id);
            if (!success) return;

            SetStatus($"Deleted task {taskItem.Id}.");
            await RefreshSyncTasksGridAsync();
        }

        private async void ResubmitTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var taskItem = button?.DataContext as ExtSyncTaskResponse;
            if (taskItem == null) return;
            if (!VerifyTokenAndVaultIsReady()) return;

            var task = await VaultAPIService.Instance.ResubmitExtSyncTaskAsync(taskItem.Id);
            if (task == null) return;

            SetStatus($"Resubmitted task {task.Id}: Status={task.Status}");
            await RefreshSyncTasksGridAsync();
        }

        private async System.Threading.Tasks.Task RefreshSyncTasksGridAsync()
        {
            int limit = int.TryParse(TaskListLimitInput.Text, out var parsed) ? parsed : 50;
            var result = await VaultAPIService.Instance.GetExtSyncTasksAsync(limit: limit);
            if (result == null) return;

            SyncTasksGrid.ItemsSource = result.Results;
            var count = result.Results?.Count ?? 0;
            SyncTasksEmptyText.Visibility = count > 0 ? Visibility.Collapsed : Visibility.Visible;
            if (count == 0) SyncTasksEmptyText.Text = "No tasks found.";
        }

        private void SetStatus(string message)
        {
            ExtSyncStatusText.Text = message ?? string.Empty;
        }

        private static List<ExtSyncInfoResponse> FormatExtSyncInfosForDisplay(List<ExtSyncInfoResponse> infos)
        {
            if (infos == null) return new List<ExtSyncInfoResponse>();

            return infos.Select(info =>
            {
                if (info == null) return null;

                var displayInfo = new ExtSyncInfoResponse
                {
                    Id = info.Id,
                    ParentId = info.ParentId,
                    Name = info.Name,
                    Value = info.Value,
                    CreateDateTime = info.CreateDateTime,
                    ParentCollectionName = info.ParentCollectionName,
                    RawValue = info.Value,
                    HasValuePopup = false,
                    ValuePopupContent = null,
                    DisplayValue = info.Value ?? string.Empty,
                    HasStatusBadge = false,
                    StatusBadgeText = null
                };

                if (string.Equals(info.Name, FusionManageDetailsInfoName, StringComparison.Ordinal))
                {
                    displayInfo.Value = ConvertSyncStatusValue(info.Value);
                    displayInfo.ValuePopupContent = BuildFusionManageDetailsPopupContent(info.Value);
                    displayInfo.HasValuePopup = !string.IsNullOrWhiteSpace(displayInfo.ValuePopupContent);
                    displayInfo.DisplayValue = displayInfo.HasValuePopup
                        ? displayInfo.ValuePopupContent
                        : (displayInfo.Value ?? string.Empty);
                }
                else if (string.Equals(info.Name, FusionManageStatusInfoName, StringComparison.Ordinal))
                {
                    displayInfo.Value = ConvertSyncStatusValue(info.Value);
                    displayInfo.DisplayValue = displayInfo.Value ?? string.Empty;
                    displayInfo.HasStatusBadge = true;
                    displayInfo.StatusBadgeText = displayInfo.Value;
                }

                return displayInfo;
            })
            .Where(info => info != null)
            .ToList();
        }

        private static string ConvertSyncStatusValue(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue)) return rawValue;

            if (!int.TryParse(rawValue.Trim(), out var statusCode))
            {
                return rawValue;
            }

            if (Enum.IsDefined(typeof(WorkResultStatus), statusCode))
            {
                return ((WorkResultStatus)statusCode).ToString();
            }

            return rawValue;
        }

        private static string BuildFusionManageDetailsPopupContent(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue)) return rawValue;
            return TryFormatJsonText(rawValue);
        }

        private static string TryFormatJsonText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            var candidate = text.Trim();
            JToken parsedToken;

            if (TryParseJsonToken(candidate, out parsedToken))
            {
                ReplaceStatusCodesWithNames(parsedToken);
                return parsedToken.ToString(Formatting.Indented);
            }

            try
            {
                var jsonString = JsonConvert.DeserializeObject<string>(candidate);
                if (!string.IsNullOrWhiteSpace(jsonString) && TryParseJsonToken(jsonString, out parsedToken))
                {
                    ReplaceStatusCodesWithNames(parsedToken);
                    return parsedToken.ToString(Formatting.Indented);
                }
            }
            catch
            {
                // Ignore parse failure and keep trying fallback paths.
            }

            var unescapedQuotes = candidate.Replace("\\\"", "\"");
            if (TryParseJsonToken(unescapedQuotes, out parsedToken))
            {
                ReplaceStatusCodesWithNames(parsedToken);
                return parsedToken.ToString(Formatting.Indented);
            }

            return text;
        }

        private static bool TryParseJsonToken(string candidate, out JToken token)
        {
            token = null;
            try
            {
                token = JToken.Parse(candidate);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void ReplaceStatusCodesWithNames(JToken token)
        {
            if (token == null) return;

            var obj = token as JObject;
            if (obj != null)
            {
                foreach (var property in obj.Properties().ToList())
                {
                    if (string.Equals(property.Name, "status", StringComparison.OrdinalIgnoreCase))
                    {
                        var rawStatus = property.Value == null ? null : property.Value.ToString();
                        var mapped = ConvertSyncStatusValue(rawStatus);
                        if (!string.IsNullOrWhiteSpace(mapped) && !string.Equals(mapped, rawStatus, StringComparison.Ordinal))
                        {
                            property.Value = mapped;
                        }
                    }

                    ReplaceStatusCodesWithNames(property.Value);
                }

                return;
            }

            var arr = token as JArray;
            if (arr != null)
            {
                foreach (var child in arr)
                {
                    ReplaceStatusCodesWithNames(child);
                }
            }
        }

        private void SetSyncInfoRefreshBusy(bool isBusy)
        {
            _isRefreshingSyncInfo = isBusy;

            var iconTransform = RefreshSyncInfoIcon?.RenderTransform as RotateTransform;
            if (iconTransform != null)
            {
                if (isBusy)
                {
                    var spin = new DoubleAnimation
                    {
                        From = 0,
                        To = 360,
                        Duration = TimeSpan.FromMilliseconds(900),
                        RepeatBehavior = RepeatBehavior.Forever
                    };
                    iconTransform.BeginAnimation(RotateTransform.AngleProperty, spin);
                }
                else
                {
                    iconTransform.BeginAnimation(RotateTransform.AngleProperty, null);
                    iconTransform.Angle = 0;
                }
            }

            var selectedItem = ItemsGrid.SelectedItem as ItemVersionResponse;
            var hasMasterId = !string.IsNullOrWhiteSpace(selectedItem?.Item?.Id);
            RefreshSyncInfoButton.IsEnabled = !isBusy && hasMasterId;
        }

        private void ClearSyncInfoCards()
        {
            SyncInfoCardsPanel.Visibility = Visibility.Collapsed;
            SyncInfoStatusCard.Visibility = Visibility.Collapsed;
            SyncInfoDetailsCard.Visibility = Visibility.Collapsed;
            SyncInfoStatusRow.Height = GridLength.Auto;
            SyncInfoSpacerRow.Height = new GridLength(8);
            SyncInfoDetailsRow.Height = new GridLength(1, GridUnitType.Star);
        }

        private void ShowSyncInfoCards(List<ExtSyncInfoResponse> infos)
        {
            if (infos == null || infos.Count == 0)
            {
                ClearSyncInfoCards();
                return;
            }

            var statusInfo = infos.FirstOrDefault(i => i != null &&
                string.Equals(i.Name, FusionManageStatusInfoName, StringComparison.Ordinal));
            var detailsInfo = infos.FirstOrDefault(i => i != null &&
                string.Equals(i.Name, FusionManageDetailsInfoName, StringComparison.Ordinal));

            if (detailsInfo == null)
            {
                detailsInfo = infos.FirstOrDefault(i => i != null && !ReferenceEquals(i, statusInfo));
            }

            SyncInfoCardsPanel.Visibility = Visibility.Visible;

            if (statusInfo != null)
            {
                SyncInfoStatusCard.Visibility = Visibility.Visible;
                SyncInfoStatusNameText.Text = statusInfo.Name ?? string.Empty;
                SyncInfoStatusCreateText.Text = statusInfo.CreateDateTime.HasValue
                    ? $"Create: {statusInfo.CreateDateTime.Value:G}"
                    : "Create: -";
                SyncInfoStatusParentText.Text = $"ParentId: {statusInfo.ParentId ?? "-"}";
                SyncInfoStatusValueText.Text = statusInfo.DisplayValue ?? statusInfo.Value ?? string.Empty;
                ApplyStatusBadge(statusInfo.StatusBadgeText);
                SyncInfoStatusRow.Height = GridLength.Auto;
                SyncInfoSpacerRow.Height = new GridLength(8);
            }
            else
            {
                SyncInfoStatusCard.Visibility = Visibility.Collapsed;
                SyncInfoStatusRow.Height = new GridLength(0);
                SyncInfoSpacerRow.Height = new GridLength(0);
            }

            if (detailsInfo != null)
            {
                SyncInfoDetailsCard.Visibility = Visibility.Visible;
                SyncInfoDetailsNameText.Text = detailsInfo.Name ?? string.Empty;
                SyncInfoDetailsCreateText.Text = detailsInfo.CreateDateTime.HasValue
                    ? $"Create: {detailsInfo.CreateDateTime.Value:G}"
                    : "Create: -";
                SyncInfoDetailsParentText.Text = $"ParentId: {detailsInfo.ParentId ?? "-"}";
                SyncInfoDetailsValueText.Text = detailsInfo.DisplayValue ?? detailsInfo.Value ?? string.Empty;
            }
            else
            {
                SyncInfoDetailsCard.Visibility = Visibility.Collapsed;
                SyncInfoDetailsValueText.Text = string.Empty;
            }

            SyncInfoDetailsRow.Height = new GridLength(1, GridUnitType.Star);
        }

        private void ApplyStatusBadge(string statusText)
        {
            if (string.IsNullOrWhiteSpace(statusText))
            {
                SyncInfoStatusBadge.Visibility = Visibility.Collapsed;
                SyncInfoStatusBadgeText.Text = string.Empty;
                return;
            }

            SyncInfoStatusBadge.Visibility = Visibility.Visible;
            SyncInfoStatusBadgeText.Text = statusText;

            switch (statusText)
            {
                case "Succeeded":
                    SyncInfoStatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1FAE5"));
                    SyncInfoStatusBadge.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34D399"));
                    break;
                case "Failed":
                    SyncInfoStatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2"));
                    SyncInfoStatusBadge.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F87171"));
                    break;
                case "PartiallySucceeded":
                    SyncInfoStatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"));
                    SyncInfoStatusBadge.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                    break;
                case "Skipped":
                    SyncInfoStatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB"));
                    SyncInfoStatusBadge.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF"));
                    break;
                default:
                    SyncInfoStatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E7FF"));
                    SyncInfoStatusBadge.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#818CF8"));
                    break;
            }
        }
    }
}
