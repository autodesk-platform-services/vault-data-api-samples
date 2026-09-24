using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using VaultDataAPISampleApp.Dialogs;
using VaultDataAPISampleApp.Features.ExternalSync.Dialogs;
using VaultDataAPISampleApp.Models;
using VaultDataAPISampleApp.Services;
using VaultDataAPISampleApp.State;

namespace VaultDataAPISampleApp.Features.ExternalSync.ViewModels
{
    public sealed partial class ExternalSyncViewModel : ObservableObject, IDisposable
    {
        private static readonly JsonSerializerOptions s_indentedJsonOptions = new()
        {
            WriteIndented = true
        };

        private const string s_defaultExtSyncConfigId =
            "Adsk.Vault.ExternalSyncTask.FusionManage";
        private const string s_defaultExtSyncWorkflowType = "Adsk.UploadItem";
        private const string s_fusionManageDetailsInfoName =
            "Adsk.FusionManage.Details";
        private const string s_fusionManageStatusInfoName =
            "Adsk.FusionManage.Status";

        private readonly IVaultDataApiService _vaultApiService;
        private readonly VaultSession _session;
        private readonly IMessageDialogService _messageDialogService;
        private readonly IExternalSyncDialogService _externalSyncDialogService;

        private CancellationTokenSource? _syncInfoCancellationTokenSource;

        [ObservableProperty]
        private string _configsText = string.Empty;

        [ObservableProperty]
        private string _hintText = "Please configure external sync mapping in Vault Client first, then click 'Load Configs' to verify.";

        [ObservableProperty]
        private bool _hasConfigs;

        [ObservableProperty]
        private int _itemLimit = 50;

        [ObservableProperty]
        private int _taskLimit = 50;

        [ObservableProperty]
        private string _itemsEmptyText = "Click 'Load Items' to get started.";

        [ObservableProperty]
        private bool _showItemsEmptyText = true;

        [ObservableProperty]
        private string _tasksEmptyText = "Click 'Load Tasks' to view tasks.";

        [ObservableProperty]
        private bool _showTasksEmptyText = true;

        [ObservableProperty]
        private string _syncInfoEmptyText = "Select an item to view sync info.";

        [ObservableProperty]
        private bool _showSyncInfoEmptyText = true;

        [ObservableProperty]
        private ItemVersionResponse? _selectedItem;

        [ObservableProperty]
        private ExtSyncInfoResponse? _statusInfo;

        [ObservableProperty]
        private ExtSyncInfoResponse? _detailsInfo;

        [ObservableProperty]
        private bool _isRefreshingSyncInfo;

        [ObservableProperty]
        private string _tasksHeader = "Sync Tasks";

        [ObservableProperty]
        private string _selectedItemText = string.Empty;

        [ObservableProperty]
        private string _status = string.Empty;

        public ExternalSyncViewModel(
            IVaultDataApiService vaultApiService,
            VaultSession session,
            IMessageDialogService messageDialogService,
            IExternalSyncDialogService externalSyncDialogService)
        {
            _vaultApiService = vaultApiService;
            _session = session;
            _messageDialogService = messageDialogService;
            _externalSyncDialogService = externalSyncDialogService;
            _session.PropertyChanged += SessionPropertyChanged;
        }

        public ObservableCollection<ItemVersionResponse> Items { get; } = [];

        public ObservableCollection<ItemVersionResponse> SelectedItems { get; } = [];

        public ObservableCollection<ExtSyncTaskResponse> Tasks { get; } = [];

        public bool HasStatusInfo => StatusInfo != null;

        public bool HasDetailsInfo => DetailsInfo != null;

        public bool HasSyncInfo => HasStatusInfo || HasDetailsInfo;

        public void Dispose()
        {
            _session.PropertyChanged -= SessionPropertyChanged;
            _syncInfoCancellationTokenSource?.Cancel();
            _syncInfoCancellationTokenSource?.Dispose();
            _syncInfoCancellationTokenSource = null;
        }

        [RelayCommand]
        private async Task LoadConfigsAsync()
        {
            await RunUiOperationAsync(LoadConfigsCoreAsync);
        }

        [RelayCommand]
        private async Task LoadItemsAsync()
        {
            await RunUiOperationAsync(LoadItemsCoreAsync);
        }

        [RelayCommand]
        private async Task LoadTasksAsync()
        {
            await RunUiOperationAsync(LoadTasksCoreAsync);
        }

        [RelayCommand(CanExecute = nameof(CanRefreshSelectedSyncInfo))]
        private async Task RefreshSelectedSyncInfoAsync()
        {
            await RunUiOperationAsync(RefreshSelectedSyncInfoCoreAsync);
        }

        [RelayCommand]
        private async Task CreateTaskAsync(ItemVersionResponse? item)
        {
            if (item == null)
            {
                return;
            }

            await RunUiOperationAsync(() => CreateTaskCoreAsync(item));
        }

        [RelayCommand]
        private async Task DeleteTaskAsync(ExtSyncTaskResponse? task)
        {
            if (task == null)
            {
                return;
            }

            await RunUiOperationAsync(() => DeleteTaskCoreAsync(task));
        }

        [RelayCommand]
        private async Task ResubmitTaskAsync(ExtSyncTaskResponse? task)
        {
            if (task == null)
            {
                return;
            }

            await RunUiOperationAsync(() => ResubmitTaskCoreAsync(task));
        }

        partial void OnSelectedItemChanged(ItemVersionResponse? value)
        {
            RefreshSelectedSyncInfoCommand.NotifyCanExecuteChanged();

            if (value == null)
            {
                ClearSyncInfo();
                SelectedItemText = string.Empty;
                return;
            }

            SelectedItemText = $"Selected: {value.Number} (Id: {value.Id})";
            _ = RunUiOperationAsync(() => RefreshSelectedItemAsync(value));
        }

        partial void OnStatusInfoChanged(ExtSyncInfoResponse? value)
        {
            OnPropertyChanged(nameof(HasStatusInfo));
            OnPropertyChanged(nameof(HasSyncInfo));
        }

        partial void OnDetailsInfoChanged(ExtSyncInfoResponse? value)
        {
            OnPropertyChanged(nameof(HasDetailsInfo));
            OnPropertyChanged(nameof(HasSyncInfo));
        }

        partial void OnIsRefreshingSyncInfoChanged(bool value)
        {
            RefreshSelectedSyncInfoCommand.NotifyCanExecuteChanged();
        }

        private async Task LoadConfigsCoreAsync()
        {
            string? vaultId = GetReadyVaultId();
            if (vaultId == null)
            {
                return;
            }

            ConfigsText = "Loading configs...";
            Dictionary<string, string>? configs =
                await _vaultApiService.GetExtSyncConfigsAsync(vaultId);
            if (!IsCurrentVault(vaultId))
            {
                return;
            }

            HasConfigs = configs is { Count: > 0 };
            if (HasConfigs)
            {
                ConfigsText = string.Join(
                    "  |  ",
                    configs!.Select(pair => $"{pair.Key}: {pair.Value}"));
                HintText = "Configs loaded successfully. You can now operate on items and tasks.";
                Status = $"Loaded {configs!.Count} config(s). Operations enabled.";
            }
            else
            {
                ConfigsText = configs == null ? "(failed to load)" : "(no configs found)";
                HintText = "No external sync configs found. Please configure mapping in Vault Client first.";
                Status = "No configs found. Operations disabled.";
            }
        }

        private async Task LoadItemsCoreAsync()
        {
            string? vaultId = GetReadyVaultId();
            if (vaultId == null)
            {
                return;
            }

            ItemsEmptyText = "Loading items...";
            ShowItemsEmptyText = true;

            CursorPaginationResponse<ItemVersionResponse>? result =
                await _vaultApiService.GetItemVersionsAsync(vaultId, ItemLimit);
            if (!IsCurrentVault(vaultId) || result == null)
            {
                return;
            }

            Items.Clear();
            foreach (ItemVersionResponse item in result.Results ?? [])
            {
                Items.Add(item);
            }

            ShowItemsEmptyText = Items.Count == 0;
            if (ShowItemsEmptyText)
            {
                ItemsEmptyText = "No items found.";
            }

            Status = $"Loaded {Items.Count} items.";
        }

        private async Task LoadTasksCoreAsync()
        {
            string? vaultId = GetReadyVaultId();
            if (vaultId == null)
            {
                return;
            }

            TasksHeader = "Sync Tasks - All";
            TasksEmptyText = "Loading tasks...";
            ShowTasksEmptyText = true;
            Tasks.Clear();

            await RefreshTasksAsync(vaultId);
            if (IsCurrentVault(vaultId))
            {
                Status = $"Listed {Tasks.Count} task(s) (limit={TaskLimit}).";
            }
        }

        private async Task RefreshSelectedItemAsync(ItemVersionResponse item)
        {
            string? vaultId = GetReadyVaultId();
            if (vaultId == null)
            {
                return;
            }

            await RefreshSyncInfoForItemAsync(
                vaultId,
                item,
                $"Loading sync info for '{item.Number}'...");
        }

        private async Task RefreshSelectedSyncInfoCoreAsync()
        {
            ItemVersionResponse? item = SelectedItem;
            if (item == null)
            {
                Status = "Select an item first, then refresh sync info.";
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

        private async Task RefreshSyncInfoForItemAsync(
            string vaultId,
            ItemVersionResponse item,
            string loadingStatusMessage)
        {
            _syncInfoCancellationTokenSource?.Cancel();
            _syncInfoCancellationTokenSource?.Dispose();
            _syncInfoCancellationTokenSource = new CancellationTokenSource();
            CancellationTokenSource cancellation = _syncInfoCancellationTokenSource;

            IsRefreshingSyncInfo = true;
            ClearSyncInfo();
            SyncInfoEmptyText = "Loading sync info...";
            ShowSyncInfoEmptyText = true;

            try
            {
                Status = loadingStatusMessage;
                string? itemId = item.Item?.Id;
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    if (cancellation.IsCancellationRequested)
                    {
                        return;
                    }

                    SyncInfoEmptyText = $"Item '{item.Number}' has no item ID.";
                    Status = $"Item '{item.Number}' does not include an item ID.";
                    return;
                }

                CursorPaginationResponse<ExtSyncInfoResponse>? result =
                    await _vaultApiService.GetItemExtSyncInfosAsync(
                        vaultId,
                        itemId);
                if (cancellation.IsCancellationRequested
                    || !IsCurrentVault(vaultId)
                    || !ReferenceEquals(SelectedItem, item))
                {
                    return;
                }

                List<ExtSyncInfoResponse>? infos = result?.Results;
                if (infos is { Count: > 0 })
                {
                    ShowSyncInfo(FormatExtSyncInfosForDisplay(infos));
                    ShowSyncInfoEmptyText = false;
                }
                else
                {
                    ClearSyncInfo();
                    SyncInfoEmptyText = $"No sync info found for '{item.Number}'.";
                    ShowSyncInfoEmptyText = true;
                }

                Status = $"Item '{item.Number}': {infos?.Count ?? 0} sync info(s).";
            }
            finally
            {
                if (!cancellation.IsCancellationRequested)
                {
                    IsRefreshingSyncInfo = false;
                }
            }
        }

        private bool CanRefreshSelectedSyncInfo()
        {
            return !IsRefreshingSyncInfo
                && !string.IsNullOrWhiteSpace(SelectedItem?.Item?.Id);
        }

        private async Task CreateTaskCoreAsync(ItemVersionResponse item)
        {
            string? vaultId = GetReadyVaultId();
            if (vaultId == null)
            {
                return;
            }

            if (!_externalSyncDialogService.ConfirmCreateTask(
                    s_defaultExtSyncConfigId,
                    s_defaultExtSyncWorkflowType))
            {
                return;
            }

            List<ItemVersionResponse> selectedItems = SelectedItems.ToList();
            if (selectedItems.Count > 1 && selectedItems.Contains(item))
            {
                List<CreateExtSyncTaskRequest> requests = [];
                foreach (ItemVersionResponse selectedItem in selectedItems)
                {
                    if (string.IsNullOrWhiteSpace(selectedItem.Id))
                    {
                        Status = "A selected item does not include an id.";
                        return;
                    }

                    requests.Add(CreateTaskRequest(selectedItem.Id));
                }

                List<ExtSyncTaskResponse>? tasks =
                    await _vaultApiService.BatchCreateExtSyncTasksAsync(
                        vaultId,
                        requests);
                if (!IsCurrentVault(vaultId) || tasks == null)
                {
                    return;
                }

                ReplaceTasks(tasks);
                Status = tasks.Count == 0
                    ? "No external sync tasks were created."
                    : $"Batch created {tasks.Count} task(s) for {selectedItems.Count} item(s).";
                return;
            }

            if (string.IsNullOrWhiteSpace(item.Id))
            {
                Status = "The selected item does not include an id.";
                return;
            }

            ExtSyncTaskResponse? task =
                await _vaultApiService.CreateExtSyncTaskAsync(
                    vaultId,
                    CreateTaskRequest(item.Id));
            if (!IsCurrentVault(vaultId) || task == null)
            {
                return;
            }

            ReplaceTasks([task]);
            Status = $"Created task  Id: {task.Id}  Status: {task.Status}";
        }

        private async Task DeleteTaskCoreAsync(ExtSyncTaskResponse task)
        {
            string? vaultId = GetReadyVaultId();
            if (vaultId == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(task.Id))
            {
                Status = "The selected task does not include an id.";
                return;
            }

            if (!_externalSyncDialogService.ConfirmDeleteTask(task.Id))
            {
                return;
            }

            bool success = await _vaultApiService.DeleteExtSyncTaskAsync(
                vaultId,
                task.Id);
            if (!IsCurrentVault(vaultId) || !success)
            {
                return;
            }

            Status = $"Deleted task {task.Id}.";
            await RefreshTasksAsync(vaultId);
        }

        private async Task ResubmitTaskCoreAsync(ExtSyncTaskResponse task)
        {
            string? vaultId = GetReadyVaultId();
            if (vaultId == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(task.Id))
            {
                Status = "The selected task does not include an id.";
                return;
            }

            ExtSyncTaskResponse? result =
                await _vaultApiService.ResubmitExtSyncTaskAsync(vaultId, task.Id);
            if (!IsCurrentVault(vaultId) || result == null)
            {
                return;
            }

            Status = $"Resubmitted task {result.Id}: Status={result.Status}";
            await RefreshTasksAsync(vaultId);
        }

        private async Task RefreshTasksAsync(string vaultId)
        {
            CursorPaginationResponse<ExtSyncTaskResponse>? result =
                await _vaultApiService.GetExtSyncTasksAsync(
                    vaultId,
                    limit: TaskLimit);
            if (!IsCurrentVault(vaultId) || result == null)
            {
                return;
            }

            ReplaceTasks(result.Results ?? []);
        }

        private void ReplaceTasks(IEnumerable<ExtSyncTaskResponse> tasks)
        {
            Tasks.Clear();
            foreach (ExtSyncTaskResponse task in tasks)
            {
                Tasks.Add(task);
            }

            ShowTasksEmptyText = Tasks.Count == 0;
            TasksEmptyText = Tasks.Count == 0
                ? "No tasks found."
                : string.Empty;
        }

        private static CreateExtSyncTaskRequest CreateTaskRequest(string entityId)
        {
            return new CreateExtSyncTaskRequest
            {
                EntityId = entityId,
                EntityClassId = "ITEM",
                ConfigId = s_defaultExtSyncConfigId,
                WorkflowType = s_defaultExtSyncWorkflowType,
                Description = $"Sync to Fusion Manage ({entityId})",
                ExecuteImmediately = true
            };
        }

        private string? GetReadyVaultId()
        {
            if (!_session.IsAuthenticated)
            {
                _messageDialogService.ShowInformation("Please login first");
                return null;
            }

            if (string.IsNullOrWhiteSpace(_session.SelectedVaultId))
            {
                _messageDialogService.ShowInformation(
                    "Please select a vault first");
                return null;
            }

            return _session.SelectedVaultId;
        }

        private bool IsCurrentVault(string vaultId)
        {
            return string.Equals(
                _session.SelectedVaultId,
                vaultId,
                StringComparison.Ordinal);
        }

        private void SessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VaultSession.SelectedVaultId))
            {
                ResetVaultState();
            }
        }

        private void ResetVaultState()
        {
            _syncInfoCancellationTokenSource?.Cancel();
            _syncInfoCancellationTokenSource?.Dispose();
            _syncInfoCancellationTokenSource = null;

            Items.Clear();
            SelectedItems.Clear();
            SelectedItem = null;
            Tasks.Clear();
            ConfigsText = string.Empty;
            HintText = "Please configure external sync mapping in Vault Client first, then click 'Load Configs' to verify.";
            HasConfigs = false;
            ItemsEmptyText = "Click 'Load Items' to get started.";
            ShowItemsEmptyText = true;
            TasksEmptyText = "Click 'Load Tasks' to view tasks.";
            ShowTasksEmptyText = true;
            SyncInfoEmptyText = "Select an item to view sync info.";
            ShowSyncInfoEmptyText = true;
            TasksHeader = "Sync Tasks";
            SelectedItemText = string.Empty;
            Status = string.Empty;
            IsRefreshingSyncInfo = false;
            ClearSyncInfo();
        }

        private async Task RunUiOperationAsync(Func<Task> operation)
        {
            try
            {
                await operation();
            }
            catch (OperationCanceledException)
            {
                Status = "Operation canceled.";
            }
            catch (Exception exception)
            {
                Status = exception.Message;
                _messageDialogService.ShowError(exception.Message);
            }
        }

        private void ClearSyncInfo()
        {
            StatusInfo = null;
            DetailsInfo = null;
        }

        private void ShowSyncInfo(List<ExtSyncInfoResponse> infos)
        {
            StatusInfo = infos.FirstOrDefault(info =>
                string.Equals(
                    info.Name,
                    s_fusionManageStatusInfoName,
                    StringComparison.Ordinal));
            DetailsInfo = infos.FirstOrDefault(info =>
                string.Equals(
                    info.Name,
                    s_fusionManageDetailsInfoName,
                    StringComparison.Ordinal));

            if (DetailsInfo == null)
            {
                DetailsInfo = infos.FirstOrDefault(info =>
                    !ReferenceEquals(info, StatusInfo));
            }
        }

        private static List<ExtSyncInfoResponse> FormatExtSyncInfosForDisplay(
            List<ExtSyncInfoResponse> infos)
        {
            return infos.Select(info =>
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

                if (string.Equals(
                        info.Name,
                        s_fusionManageDetailsInfoName,
                        StringComparison.Ordinal))
                {
                    displayInfo.Value = ConvertSyncStatusValue(info.Value);
                    displayInfo.ValuePopupContent =
                        BuildFusionManageDetailsPopupContent(info.Value);
                    displayInfo.HasValuePopup =
                        !string.IsNullOrWhiteSpace(displayInfo.ValuePopupContent);
                    displayInfo.DisplayValue = displayInfo.HasValuePopup
                        ? displayInfo.ValuePopupContent ?? string.Empty
                        : displayInfo.Value ?? string.Empty;
                }
                else if (string.Equals(
                             info.Name,
                             s_fusionManageStatusInfoName,
                             StringComparison.Ordinal))
                {
                    displayInfo.Value = ConvertSyncStatusValue(info.Value);
                    displayInfo.DisplayValue = displayInfo.Value ?? string.Empty;
                    displayInfo.HasStatusBadge = true;
                    displayInfo.StatusBadgeText = displayInfo.Value;
                }

                return displayInfo;
            }).ToList();
        }

        private static string? ConvertSyncStatusValue(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return rawValue;
            }

            if (!int.TryParse(rawValue.Trim(), out int statusCode))
            {
                return rawValue;
            }

            return Enum.IsDefined(typeof(WorkResultStatus), statusCode)
                ? ((WorkResultStatus)statusCode).ToString()
                : rawValue;
        }

        private static string? BuildFusionManageDetailsPopupContent(string? rawValue)
        {
            return string.IsNullOrWhiteSpace(rawValue)
                ? rawValue
                : TryFormatJsonText(rawValue);
        }

        private static string TryFormatJsonText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            string candidate = text.Trim();
            if (TryParseJsonNode(candidate, out JsonNode? parsedNode))
            {
                ReplaceStatusCodesWithNames(parsedNode);
                return parsedNode.ToJsonString(s_indentedJsonOptions);
            }

            try
            {
                string? jsonString = JsonSerializer.Deserialize<string>(candidate);
                if (!string.IsNullOrWhiteSpace(jsonString)
                    && TryParseJsonNode(jsonString, out parsedNode))
                {
                    ReplaceStatusCodesWithNames(parsedNode);
                    return parsedNode.ToJsonString(s_indentedJsonOptions);
                }
            }
            catch (JsonException)
            {
                // Keep trying the supported fallback formats.
            }

            string unescapedQuotes = candidate.Replace("\\\"", "\"");
            if (TryParseJsonNode(unescapedQuotes, out parsedNode))
            {
                ReplaceStatusCodesWithNames(parsedNode);
                return parsedNode.ToJsonString(s_indentedJsonOptions);
            }

            return text;
        }

        private static bool TryParseJsonNode(
            string candidate,
            [NotNullWhen(true)] out JsonNode? node)
        {
            node = null;
            try
            {
                node = JsonNode.Parse(candidate);
                return node != null;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static void ReplaceStatusCodesWithNames(JsonNode node)
        {
            if (node is JsonObject jsonObject)
            {
                foreach (KeyValuePair<string, JsonNode?> property in jsonObject.ToList())
                {
                    if (string.Equals(
                            property.Key,
                            "status",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        string? rawStatus = property.Value?.ToString();
                        string? mapped = ConvertSyncStatusValue(rawStatus);
                        if (!string.IsNullOrWhiteSpace(mapped)
                            && !string.Equals(
                                mapped,
                                rawStatus,
                                StringComparison.Ordinal))
                        {
                            jsonObject[property.Key] = mapped;
                        }
                    }

                    if (jsonObject[property.Key] is JsonNode propertyValue)
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
                    if (child != null)
                    {
                        ReplaceStatusCodesWithNames(child);
                    }
                }
            }
        }

        private enum WorkResultStatus
        {
            Unknown = 0,
            Succeeded = 1,
            Failed = 2,
            PartiallySucceeded = 3,
            Skipped = 4
        }
    }
}
