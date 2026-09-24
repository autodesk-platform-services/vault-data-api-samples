using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using VaultDataAPISampleApp.Models;

namespace VaultDataAPISampleApp
{
    /// <summary>
    /// Interaction logic for ExternalSyncTabControl.xaml
    /// </summary>
    public partial class ExternalSyncTabControl : UserControl
    {
        private static readonly JsonSerializerOptions s_indentedJsonOptions = new()
        {
            WriteIndented = true
        };

        private const string s_defaultExtSyncConfigId = "Adsk.Vault.ExternalSyncTask.FusionManage";
        private const string s_defaultExtSyncWorkflowType = "Adsk.UploadItem";
        private const string s_fusionManageDetailsInfoName = "Adsk.FusionManage.Details";
        private const string s_fusionManageStatusInfoName = "Adsk.FusionManage.Status";

        private enum WorkResultStatus
        {
            Unknown = 0,
            Succeeded = 1,
            Failed = 2,
            PartiallySucceeded = 3,
            Skipped = 4
        }

        private readonly ObservableCollection<ItemVersionResponse> _itemsData = new ObservableCollection<ItemVersionResponse>();
        private readonly VaultAPIService _vaultAPIService;

        private bool _isRefreshingSyncInfo;
        private CancellationTokenSource? _syncInfoCts;
        private string? _selectedVaultId;

        public ExternalSyncTabControl(VaultAPIService vaultAPIService)
        {
            InitializeComponent();
            _vaultAPIService = vaultAPIService;
            DataContext = null;
            ItemsGrid.DataContext = null;
            SyncTasksGrid.DataContext = null;
            ItemsGrid.ItemsSource = _itemsData;
        }

        internal string? SelectedVaultId
        {
            get
            {
                return _selectedVaultId;
            }
            set
            {
                if (string.Equals(_selectedVaultId, value, StringComparison.Ordinal))
                {
                    return;
                }

                _selectedVaultId = value;
                ResetVaultState();
            }
        }

        private string? GetReadyVaultId()
        {
            if (!_vaultAPIService.HasAccessToken)
            {
                MessageBox.Show("Please login first");
                return null;
            }

            if (string.IsNullOrWhiteSpace(SelectedVaultId))
            {
                MessageBox.Show("Please select a vault first");
                return null;
            }

            return SelectedVaultId;
        }

        private async void LoadConfigs_Click(object sender, RoutedEventArgs e)
        {
            await RunUiOperationAsync(LoadConfigsAsync);
        }

        private async Task LoadConfigsAsync()
        {
            string? vaultId = GetReadyVaultId();
            if (vaultId == null)
            {
                return;
            }

            ExtSyncConfigsText.Text = "Loading configs...";
            var configs = await _vaultAPIService.GetExtSyncConfigsAsync(vaultId);
            if (!IsCurrentVault(vaultId))
            {
                return;
            }

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
            await RunUiOperationAsync(ListTasksAsync);
        }

        private async Task ListTasksAsync()
        {
            string? vaultId = GetReadyVaultId();
            if (vaultId == null)
            {
                return;
            }

            int limit = int.TryParse(TaskListLimitInput.Text, out var parsed) ? parsed : 50;
            SyncTasksGroupBox.Header = "Sync Tasks - All";
            SyncTasksEmptyText.Text = "Loading tasks...";
            SyncTasksEmptyText.Visibility = Visibility.Visible;
            SyncTasksGrid.ItemsSource = null;

            var result = await _vaultAPIService.GetExtSyncTasksAsync(vaultId, limit: limit);
            if (!IsCurrentVault(vaultId))
            {
                return;
            }

            if (result == null)
            {
                return;
            }

            SyncTasksGrid.ItemsSource = result.Results;
            var count = result.Results?.Count ?? 0;
            SyncTasksEmptyText.Visibility = count > 0 ? Visibility.Collapsed : Visibility.Visible;
            if (count == 0)
            {
                SyncTasksEmptyText.Text = "No tasks found.";
            }

            SetStatus($"Listed {count} task(s) (limit={limit}).");
        }

        private async void LoadItems_Click(object sender, RoutedEventArgs e)
        {
            await RunUiOperationAsync(LoadItemsAsync);
        }

        private async Task LoadItemsAsync()
        {
            string? vaultId = GetReadyVaultId();
            if (vaultId == null)
            {
                return;
            }

            int limit = int.TryParse(ItemListLimitInput.Text, out var parsed) ? parsed : 50;
            ItemsEmptyText.Text = "Loading items...";
            ItemsEmptyText.Visibility = Visibility.Visible;

            var result = await _vaultAPIService.GetItemVersionsAsync(vaultId, limit: limit);
            if (!IsCurrentVault(vaultId))
            {
                return;
            }

            if (result == null)
            {
                return;
            }

            _itemsData.Clear();
            foreach (var item in result.Results ?? new List<ItemVersionResponse>())
            {
                _itemsData.Add(item);
            }

            ItemsEmptyText.Visibility = _itemsData.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            if (_itemsData.Count == 0)
            {
                ItemsEmptyText.Text = "No items found.";
            }

            SetStatus($"Loaded {_itemsData.Count} items.");
        }

        private async void ItemsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await RunUiOperationAsync(HandleItemsGridSelectionChangedAsync);
        }

        private async Task HandleItemsGridSelectionChangedAsync()
        {
            if (ItemsGrid.SelectedItem is not ItemVersionResponse item)
            {
                RefreshSyncInfoButton.IsEnabled = false;
                ClearSyncInfoCards();
                SyncInfoEmptyText.Text = "Select an item to view sync info.";
                SyncInfoEmptyText.Visibility = Visibility.Visible;
                ExtSyncSelectedItemText.Text = string.Empty;
                return;
            }

            string? vaultId = GetReadyVaultId();
            if (vaultId == null)
            {
                return;
            }

            var hasMasterId = !string.IsNullOrWhiteSpace(item.Item?.Id);
            RefreshSyncInfoButton.IsEnabled = !_isRefreshingSyncInfo && hasMasterId;
            ExtSyncSelectedItemText.Text = $"Selected: {item.Number} (Id: {item.Id})";
            await RefreshSyncInfoForItemAsync(
                vaultId,
                item,
                $"Loading sync info for '{item.Number}'...");
        }

        private async Task RefreshSyncInfoForItemAsync(
            string vaultId,
            ItemVersionResponse item,
            string loadingStatusMessage)
        {
            _syncInfoCts?.Cancel();
            _syncInfoCts?.Dispose();
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
                    if (cts.IsCancellationRequested)
                    {
                        return;
                    }

                    ClearSyncInfoCards();
                    SyncInfoEmptyText.Text = $"Item '{item.Number}' has no master id.";
                    SyncInfoEmptyText.Visibility = Visibility.Visible;
                    SetStatus($"Item '{item.Number}' does not include a master id.");
                    return;
                }

                var syncInfoResult = await _vaultAPIService.GetItemExtSyncInfosAsync(
                    vaultId,
                    itemMasterId);
                if (cts.IsCancellationRequested || !IsCurrentVault(vaultId))
                {
                    return;
                }

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
                {
                    SetSyncInfoRefreshBusy(false);
                }
            }
        }

        private async void RefreshSyncInfoButton_Click(object sender, RoutedEventArgs e)
        {
            await RunUiOperationAsync(RefreshSelectedSyncInfoAsync);
        }

        private async Task RefreshSelectedSyncInfoAsync()
        {
            if (ItemsGrid.SelectedItem is not ItemVersionResponse item)
            {
                SetStatus("Select an item first, then refresh sync info.");
                return;
            }

            string? vaultId = GetReadyVaultId();
            if (vaultId == null)
            {
                return;
            }

            await RefreshSyncInfoForItemAsync(
                vaultId,
                item,
                $"Refreshing sync info for '{item.Number}'...");
        }

        private async void CreateTaskButton_Click(object sender, RoutedEventArgs e)
        {
            await RunUiOperationAsync(() => CreateTaskAsync(sender));
        }

        private async Task CreateTaskAsync(object sender)
        {
            var button = sender as Button;
            if (button?.DataContext is not ItemVersionResponse item)
            {
                return;
            }

            string? vaultId = GetReadyVaultId();
            if (vaultId == null)
            {
                return;
            }

            var dialog = new CreateTaskConfirmWindow(s_defaultExtSyncConfigId, s_defaultExtSyncWorkflowType);
            var ownerWindow = Window.GetWindow(this);
            if (ownerWindow != null)
            {
                dialog.Owner = ownerWindow;
            }

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var configId = string.IsNullOrWhiteSpace(dialog.ConfigId)
                ? s_defaultExtSyncConfigId
                : dialog.ConfigId.Trim();
            var workflowType = string.IsNullOrWhiteSpace(dialog.WorkflowType)
                ? s_defaultExtSyncWorkflowType
                : dialog.WorkflowType.Trim();

            var selectedItems = ItemsGrid.SelectedItems.Cast<ItemVersionResponse>().ToList();
            if (selectedItems.Count > 1 && selectedItems.Contains(item))
            {
                List<CreateExtSyncTaskRequest> requests = [];
                foreach (ItemVersionResponse selectedItem in selectedItems)
                {
                    string? entityId = selectedItem.Id;
                    if (string.IsNullOrWhiteSpace(entityId))
                    {
                        SetStatus("A selected item does not include an id.");
                        return;
                    }

                    requests.Add(new CreateExtSyncTaskRequest
                    {
                        EntityId = entityId,
                        EntityClassId = "ITEM",
                        ConfigId = configId,
                        WorkflowType = workflowType,
                        Description = $"Sync to Fusion Manage ({entityId})",
                        ExecuteImmediately = true
                    });
                }

                var tasks = await _vaultAPIService.BatchCreateExtSyncTasksAsync(
                    vaultId,
                    requests);
                if (!IsCurrentVault(vaultId))
                {
                    return;
                }

                if (tasks == null)
                {
                    return;
                }

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
                string? entityId = item.Id;
                if (string.IsNullOrWhiteSpace(entityId))
                {
                    SetStatus("The selected item does not include an id.");
                    return;
                }

                var request = new CreateExtSyncTaskRequest
                {
                    EntityId = entityId,
                    EntityClassId = "ITEM",
                    ConfigId = configId,
                    WorkflowType = workflowType,
                    Description = $"Sync to Fusion Manage ({entityId})",
                    ExecuteImmediately = true
                };

                var task = await _vaultAPIService.CreateExtSyncTaskAsync(
                    vaultId,
                    request);
                if (!IsCurrentVault(vaultId))
                {
                    return;
                }

                if (task == null)
                {
                    return;
                }

                SyncTasksGrid.ItemsSource = new List<ExtSyncTaskResponse> { task };
                SyncTasksEmptyText.Visibility = Visibility.Collapsed;
                SetStatus($"Created task  Id: {task.Id}  Status: {task.Status}");
            }
        }

        private async void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
        {
            await RunUiOperationAsync(() => DeleteTaskAsync(sender));
        }

        private async Task DeleteTaskAsync(object sender)
        {
            var button = sender as Button;
            if (button?.DataContext is not ExtSyncTaskResponse taskItem)
            {
                return;
            }

            string? vaultId = GetReadyVaultId();
            if (vaultId == null)
            {
                return;
            }

            string? taskId = taskItem.Id;
            if (string.IsNullOrWhiteSpace(taskId))
            {
                SetStatus("The selected task does not include an id.");
                return;
            }

            var confirm = MessageBox.Show($"Are you sure you want to delete task '{taskId}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var success = await _vaultAPIService.DeleteExtSyncTaskAsync(vaultId, taskId);
            if (!IsCurrentVault(vaultId))
            {
                return;
            }

            if (!success)
            {
                return;
            }

            SetStatus($"Deleted task {taskId}.");
            await RefreshSyncTasksGridAsync(vaultId);
        }

        private async void ResubmitTaskButton_Click(object sender, RoutedEventArgs e)
        {
            await RunUiOperationAsync(() => ResubmitTaskAsync(sender));
        }

        private async Task ResubmitTaskAsync(object sender)
        {
            var button = sender as Button;
            if (button?.DataContext is not ExtSyncTaskResponse taskItem)
            {
                return;
            }

            string? vaultId = GetReadyVaultId();
            if (vaultId == null)
            {
                return;
            }

            string? taskId = taskItem.Id;
            if (string.IsNullOrWhiteSpace(taskId))
            {
                SetStatus("The selected task does not include an id.");
                return;
            }

            var task = await _vaultAPIService.ResubmitExtSyncTaskAsync(vaultId, taskId);
            if (!IsCurrentVault(vaultId))
            {
                return;
            }

            if (task == null)
            {
                return;
            }

            SetStatus($"Resubmitted task {task.Id}: Status={task.Status}");
            await RefreshSyncTasksGridAsync(vaultId);
        }

        private async Task RefreshSyncTasksGridAsync(string vaultId)
        {
            int limit = int.TryParse(TaskListLimitInput.Text, out var parsed) ? parsed : 50;
            var result = await _vaultAPIService.GetExtSyncTasksAsync(vaultId, limit: limit);
            if (!IsCurrentVault(vaultId))
            {
                return;
            }

            if (result == null)
            {
                return;
            }

            SyncTasksGrid.ItemsSource = result.Results;
            var count = result.Results?.Count ?? 0;
            SyncTasksEmptyText.Visibility = count > 0 ? Visibility.Collapsed : Visibility.Visible;
            if (count == 0)
            {
                SyncTasksEmptyText.Text = "No tasks found.";
            }
        }

        private bool IsCurrentVault(string vaultId)
        {
            return string.Equals(_selectedVaultId, vaultId, StringComparison.Ordinal);
        }

        private void ResetVaultState()
        {
            _syncInfoCts?.Cancel();
            _syncInfoCts?.Dispose();
            _syncInfoCts = null;

            _itemsData.Clear();
            ItemsGrid.SelectedItem = null;
            SyncTasksGrid.ItemsSource = null;
            ExtSyncConfigsText.Text = string.Empty;
            ExtSyncSelectedItemText.Text = string.Empty;
            ExtSyncHintText.Text = "Please configure external sync mapping in Vault Client first, then click 'Load Configs' to verify.";
            ExtSyncHintText.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#D13438"));
            SyncTasksGroupBox.Header = "Sync Tasks";

            LoadItemsPanel.IsEnabled = false;
            ExtSyncContentPanel.IsEnabled = false;
            ExtSyncContentPanel.Opacity = 0.5;

            ItemsEmptyText.Text = "Click 'Load Items' to get started.";
            ItemsEmptyText.Visibility = Visibility.Visible;
            SyncTasksEmptyText.Text = "Click 'Load Tasks' to view tasks.";
            SyncTasksEmptyText.Visibility = Visibility.Visible;
            SyncInfoEmptyText.Text = "Select an item to view sync info.";
            SyncInfoEmptyText.Visibility = Visibility.Visible;
            ClearSyncInfoCards();
            SetSyncInfoRefreshBusy(false);
            SetStatus(string.Empty);
        }

        private async Task RunUiOperationAsync(Func<Task> operation)
        {
            try
            {
                await operation();
            }
            catch (OperationCanceledException)
            {
                SetStatus("Operation canceled.");
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message);
                MessageBox.Show(
                    exception.Message,
                    "Vault Data API",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SetStatus(string message)
        {
            ExtSyncStatusText.Text = message;
        }

        private static List<ExtSyncInfoResponse> FormatExtSyncInfosForDisplay(List<ExtSyncInfoResponse> infos)
        {
            var formattedInfos = infos.Select(info =>
            {
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

                if (string.Equals(info.Name, s_fusionManageDetailsInfoName, StringComparison.Ordinal))
                {
                    displayInfo.Value = ConvertSyncStatusValue(info.Value);
                    displayInfo.ValuePopupContent = BuildFusionManageDetailsPopupContent(info.Value);
                    displayInfo.HasValuePopup = !string.IsNullOrWhiteSpace(displayInfo.ValuePopupContent);
                    displayInfo.DisplayValue = displayInfo.HasValuePopup
                        ? displayInfo.ValuePopupContent ?? string.Empty
                        : (displayInfo.Value ?? string.Empty);
                }
                else if (string.Equals(info.Name, s_fusionManageStatusInfoName, StringComparison.Ordinal))
                {
                    displayInfo.Value = ConvertSyncStatusValue(info.Value);
                    displayInfo.DisplayValue = displayInfo.Value ?? string.Empty;
                    displayInfo.HasStatusBadge = true;
                    displayInfo.StatusBadgeText = displayInfo.Value;
                }

                return displayInfo;
            });

            return formattedInfos.ToList();
        }

        private static string? ConvertSyncStatusValue(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return rawValue;
            }

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

        private static string? BuildFusionManageDetailsPopupContent(string? rawValue)
        {
            return string.IsNullOrWhiteSpace(rawValue) ? rawValue : TryFormatJsonText(rawValue);
        }

        private static string TryFormatJsonText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            var candidate = text.Trim();
            JsonNode? parsedNode;

            if (TryParseJsonNode(candidate, out parsedNode))
            {
                ReplaceStatusCodesWithNames(parsedNode);
                return parsedNode.ToJsonString(s_indentedJsonOptions);
            }

            try
            {
                var jsonString = JsonSerializer.Deserialize<string>(candidate);
                if (!string.IsNullOrWhiteSpace(jsonString) && TryParseJsonNode(jsonString, out parsedNode))
                {
                    ReplaceStatusCodesWithNames(parsedNode);
                    return parsedNode.ToJsonString(s_indentedJsonOptions);
                }
            }
            catch
            {
                // Ignore parse failure and keep trying fallback paths.
            }

            var unescapedQuotes = candidate.Replace("\\\"", "\"");
            if (TryParseJsonNode(unescapedQuotes, out parsedNode))
            {
                ReplaceStatusCodesWithNames(parsedNode);
                return parsedNode.ToJsonString(s_indentedJsonOptions);
            }

            return text;
        }

        private static bool TryParseJsonNode(
            string candidate,
            [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out JsonNode? node)
        {
            node = null;
            try
            {
                node = JsonNode.Parse(candidate);
                return node is not null;
            }
            catch
            {
                return false;
            }
        }

        private static void ReplaceStatusCodesWithNames(JsonNode node)
        {
            if (node is JsonObject obj)
            {
                foreach (KeyValuePair<string, JsonNode?> property in obj.ToList())
                {
                    if (string.Equals(property.Key, "status", StringComparison.OrdinalIgnoreCase))
                    {
                        var rawStatus = property.Value?.ToString();
                        var mapped = ConvertSyncStatusValue(rawStatus);
                        if (!string.IsNullOrWhiteSpace(mapped) && !string.Equals(mapped, rawStatus, StringComparison.Ordinal))
                        {
                            obj[property.Key] = mapped;
                        }
                    }

                    if (obj[property.Key] is JsonNode propertyValue)
                    {
                        ReplaceStatusCodesWithNames(propertyValue);
                    }
                }

                return;
            }

            if (node is JsonArray array)
            {
                foreach (JsonNode? child in array)
                {
                    if (child is not null)
                    {
                        ReplaceStatusCodesWithNames(child);
                    }
                }
            }
        }

        private void SetSyncInfoRefreshBusy(bool isBusy)
        {
            _isRefreshingSyncInfo = isBusy;

            if (RefreshSyncInfoIcon?.RenderTransform is RotateTransform iconTransform)
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
            if (infos.Count == 0)
            {
                ClearSyncInfoCards();
                return;
            }

            var statusInfo = infos.FirstOrDefault(i =>
                string.Equals(i.Name, s_fusionManageStatusInfoName, StringComparison.Ordinal));
            var detailsInfo = infos.FirstOrDefault(i =>
                string.Equals(i.Name, s_fusionManageDetailsInfoName, StringComparison.Ordinal));

            if (detailsInfo == null)
            {
                detailsInfo = infos.FirstOrDefault(i => !ReferenceEquals(i, statusInfo));
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
                SyncInfoStatusValueText.Text = statusInfo.DisplayValue;
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
                SyncInfoDetailsValueText.Text = detailsInfo.DisplayValue;
            }
            else
            {
                SyncInfoDetailsCard.Visibility = Visibility.Collapsed;
                SyncInfoDetailsValueText.Text = string.Empty;
            }

            SyncInfoDetailsRow.Height = new GridLength(1, GridUnitType.Star);
        }

        private void ApplyStatusBadge(string? statusText)
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
