using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using VaultDataAPISampleApp.Dialogs;
using VaultDataAPISampleApp.Features.FileUpload.Dialogs;
using VaultDataAPISampleApp.Features.FileUpload.Services;
using VaultDataAPISampleApp.Models;
using VaultDataAPISampleApp.Services;
using VaultDataAPISampleApp.State;

namespace VaultDataAPISampleApp.Features.FileUpload.ViewModels
{
    public sealed class FileUploadViewModel : ObservableObject, IDisposable
    {
        private readonly IFileUploadService _fileUploadService;
        private readonly IVaultDataApiService _vaultApiService;
        private readonly ILocalFilePickerService _filePickerService;
        private readonly IMessageDialogService _messageDialogService;
        private readonly VaultSession _session;

        private string _localFilePath = string.Empty;
        private string _comment = string.Empty;
        private string _status = "Expand a folder to browse Vault contents.";
        private VaultContentNode? _selectedNode;
        private double _progressPercentage;
        private bool _checkoutFirst = true;
        private bool _keepCheckedOut;
        private bool _isBusy;
        private bool _isLoadingRoot;
        private bool _isProgressIndeterminate;
        private int _loadVersion;

        public FileUploadViewModel(
            IFileUploadService fileUploadService,
            IVaultDataApiService vaultApiService,
            ILocalFilePickerService filePickerService,
            IMessageDialogService messageDialogService,
            VaultSession session)
        {
            _fileUploadService = fileUploadService;
            _vaultApiService = vaultApiService;
            _filePickerService = filePickerService;
            _messageDialogService = messageDialogService;
            _session = session;

            BrowseCommand = new RelayCommand(Browse, () => !IsBusy);
            RefreshTreeCommand = new AsyncRelayCommand(
                LoadRootAsync,
                CanLoadRoot);
            AddFileCommand = new AsyncRelayCommand(
                AddFileAsync,
                CanAddFile);
            CheckinFileCommand = new AsyncRelayCommand(
                CheckinFileAsync,
                CanCheckinFile);
            CancelCommand = new RelayCommand(Cancel, () => IsBusy);

            _session.PropertyChanged += SessionPropertyChanged;
            Status = _session.IsAuthenticated
                && !string.IsNullOrWhiteSpace(_session.SelectedVaultId)
                ? "Select Refresh Tree to browse Vault contents."
                : "Sign in and select a Vault first.";
        }

        public ObservableCollection<VaultContentNode> VaultContents { get; } = [];

        public string LocalFilePath
        {
            get
            {
                return _localFilePath;
            }
            set
            {
                if (SetProperty(ref _localFilePath, value))
                {
                    NotifyCommandStates();
                }
            }
        }

        public VaultContentNode? SelectedNode
        {
            get
            {
                return _selectedNode;
            }
            set
            {
                if (SetProperty(ref _selectedNode, value))
                {
                    AddFileCommand.NotifyCanExecuteChanged();
                    CheckinFileCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string Comment
        {
            get
            {
                return _comment;
            }
            set
            {
                SetProperty(ref _comment, value);
            }
        }

        public bool CheckoutFirst
        {
            get
            {
                return _checkoutFirst;
            }
            set
            {
                SetProperty(ref _checkoutFirst, value);
            }
        }

        public bool KeepCheckedOut
        {
            get
            {
                return _keepCheckedOut;
            }
            set
            {
                SetProperty(ref _keepCheckedOut, value);
            }
        }

        public string Status
        {
            get
            {
                return _status;
            }
            private set
            {
                SetProperty(ref _status, value);
            }
        }

        public double ProgressPercentage
        {
            get
            {
                return _progressPercentage;
            }
            private set
            {
                SetProperty(ref _progressPercentage, value);
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
                if (SetProperty(ref _isBusy, value))
                {
                    NotifyCommandStates();
                }
            }
        }

        public bool IsLoadingRoot
        {
            get
            {
                return _isLoadingRoot;
            }
            private set
            {
                if (SetProperty(ref _isLoadingRoot, value))
                {
                    NotifyCommandStates();
                }
            }
        }

        public bool IsProgressIndeterminate
        {
            get
            {
                return _isProgressIndeterminate;
            }
            private set
            {
                SetProperty(ref _isProgressIndeterminate, value);
            }
        }

        public IRelayCommand BrowseCommand { get; }

        public IAsyncRelayCommand RefreshTreeCommand { get; }

        public IAsyncRelayCommand AddFileCommand { get; }

        public IAsyncRelayCommand CheckinFileCommand { get; }

        public IRelayCommand CancelCommand { get; }

        public void Dispose()
        {
            _session.PropertyChanged -= SessionPropertyChanged;
        }

        private void Browse()
        {
            string? path = _filePickerService.SelectFile();
            if (!string.IsNullOrWhiteSpace(path))
            {
                LocalFilePath = path;
            }
        }

        private bool CanAddFile()
        {
            FolderResponse? folder = SelectedNode?.Folder;
            return CanUpload()
                && folder is { IsCloaked: false, IsReadOnly: false }
                && !string.IsNullOrWhiteSpace(folder.Id);
        }

        private bool CanCheckinFile()
        {
            FileVersionResponse? file = SelectedNode?.File;
            return CanUpload()
                && file is { IsCloaked: false, IsReadOnly: false }
                && !string.IsNullOrWhiteSpace(file.Id);
        }

        private bool CanUpload()
        {
            return !IsBusy
                && !IsLoadingRoot
                && _session.IsAuthenticated
                && !string.IsNullOrWhiteSpace(_session.SelectedVaultId)
                && !string.IsNullOrWhiteSpace(LocalFilePath);
        }

        private async Task AddFileAsync(CancellationToken cancellationToken)
        {
            string? vaultId = _session.SelectedVaultId;
            FolderResponse? selectedFolder = SelectedNode?.Folder;
            if (string.IsNullOrWhiteSpace(vaultId) || selectedFolder == null)
            {
                return;
            }

            if (!long.TryParse(selectedFolder.Id, out long folderId)
                || folderId <= 0)
            {
                _messageDialogService.ShowError(
                    "Vault returned an invalid identifier for the selected folder.");
                return;
            }

            string destination = selectedFolder.FullName
                ?? selectedFolder.Name
                ?? "the selected folder";

            await RunUploadAsync(
                async progress =>
                {
                    await _fileUploadService.AddFileAsync(
                        vaultId,
                        folderId,
                        LocalFilePath,
                        Comment,
                        progress,
                        cancellationToken);
                    Status = $"Added {Path.GetFileName(LocalFilePath)} to {destination}.";
                    _messageDialogService.ShowInformation(Status);
                });
        }

        private async Task CheckinFileAsync(CancellationToken cancellationToken)
        {
            string? vaultId = _session.SelectedVaultId;
            FileVersionResponse? selectedFile = SelectedNode?.File;
            if (string.IsNullOrWhiteSpace(vaultId)
                || selectedFile == null)
            {
                return;
            }

            await RunUploadAsync(
                async progress =>
                {
                    string? fileId = selectedFile.File?.Id;
                    if (string.IsNullOrWhiteSpace(fileId))
                    {
                        progress.Report(new FileUploadProgress(
                            "Loading file details..."));
                        FileVersionResponse fileDetails =
                            await _vaultApiService.GetFileVersionAsync(
                                vaultId,
                                selectedFile.Id,
                                cancellationToken);
                        fileId = fileDetails.File?.Id;
                    }

                    if (string.IsNullOrWhiteSpace(fileId))
                    {
                        throw new InvalidOperationException(
                            "Vault did not return an identifier for the selected file.");
                    }

                    FileVersionResponse result =
                        await _fileUploadService.CheckinFileAsync(
                            vaultId,
                            fileId,
                            LocalFilePath,
                            Comment,
                            CheckoutFirst,
                            KeepCheckedOut,
                            progress,
                            cancellationToken);
                    Status = $"Checked in {result.Name} as version {result.Version}.";
                    _messageDialogService.ShowInformation(Status);
                });
        }

        private bool CanLoadRoot()
        {
            return !IsBusy
                && !IsLoadingRoot
                && _session.IsAuthenticated
                && !string.IsNullOrWhiteSpace(_session.SelectedVaultId);
        }

        private async Task LoadRootAsync()
        {
            string? vaultId = _session.SelectedVaultId;
            if (string.IsNullOrWhiteSpace(vaultId))
            {
                return;
            }

            int loadVersion = ++_loadVersion;
            IsLoadingRoot = true;
            Status = "Loading the Vault root...";

            try
            {
                FolderResponse rootFolder =
                    await _vaultApiService.GetFolderAsync(vaultId, "root");

                if (loadVersion != _loadVersion
                    || !string.Equals(
                        vaultId,
                        _session.SelectedVaultId,
                        StringComparison.Ordinal))
                {
                    return;
                }

                VaultContents.Clear();
                VaultContents.Add(CreateFolderNode(rootFolder));
                SelectedNode = null;
                Status = "Expand folders to load their direct contents.";
            }
            catch (Exception exception)
            {
                if (loadVersion == _loadVersion)
                {
                    Status = "Could not load the Vault root.";
                    _messageDialogService.ShowError(exception.Message);
                }
            }
            finally
            {
                if (loadVersion == _loadVersion)
                {
                    IsLoadingRoot = false;
                }
            }
        }

        private async Task<IReadOnlyList<VaultContentNode>> LoadFolderChildrenAsync(
            VaultContentNode node)
        {
            string? vaultId = _session.SelectedVaultId;
            string? folderId = node.Folder?.Id;
            if (string.IsNullOrWhiteSpace(vaultId)
                || string.IsNullOrWhiteSpace(folderId))
            {
                return [];
            }

            FolderContentsResponse contents =
                await _vaultApiService.GetFolderContentsAsync(
                    vaultId,
                    folderId);

            var children = new List<VaultContentNode>();
            children.AddRange(contents.Folders
                .Where(folder => !folder.IsCloaked)
                .OrderBy(folder => folder.Name)
                .Select(CreateFolderNode));
            children.AddRange(contents.Files
                .Where(file => !file.IsCloaked)
                .OrderBy(file => file.Name)
                .Select(file => VaultContentNode.CreateFile(
                    file,
                    SelectNode)));
            return children;
        }

        private VaultContentNode CreateFolderNode(FolderResponse folder)
        {
            return VaultContentNode.CreateFolder(
                folder,
                LoadFolderChildrenAsync,
                ShowFolderLoadError,
                SelectNode);
        }

        private void SelectNode(VaultContentNode node)
        {
            SelectedNode = node;
        }

        private void ShowFolderLoadError(Exception exception)
        {
            _messageDialogService.ShowError(exception.Message);
        }

        private async Task RunUploadAsync(
            Func<IProgress<FileUploadProgress>, Task> operation)
        {
            IsBusy = true;
            ProgressPercentage = 0;
            IsProgressIndeterminate = true;
            var progress = new Progress<FileUploadProgress>(UpdateProgress);

            try
            {
                await operation(progress);
            }
            catch (OperationCanceledException)
            {
                Status = "The file operation was canceled.";
            }
            catch (Exception exception)
            {
                Status = "The file operation failed.";
                _messageDialogService.ShowError(exception.Message);
            }
            finally
            {
                IsBusy = false;
                IsProgressIndeterminate = false;
            }
        }

        private void UpdateProgress(FileUploadProgress progress)
        {
            Status = progress.Status;
            IsProgressIndeterminate = progress.TotalBytes <= 0;
            ProgressPercentage = progress.TotalBytes <= 0
                ? 0
                : (double)progress.BytesTransferred / progress.TotalBytes * 100;
        }

        private void Cancel()
        {
            AddFileCommand.Cancel();
            CheckinFileCommand.Cancel();
        }

        private void SessionPropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VaultSession.SelectedVaultId)
                || e.PropertyName == nameof(VaultSession.IsAuthenticated))
            {
                _loadVersion++;
                IsLoadingRoot = false;
                VaultContents.Clear();
                SelectedNode = null;
                Status = _session.IsAuthenticated
                    && !string.IsNullOrWhiteSpace(_session.SelectedVaultId)
                    ? "Select Refresh Tree to browse Vault contents."
                    : "Sign in and select a Vault first.";
                NotifyCommandStates();
            }
        }

        private void NotifyCommandStates()
        {
            BrowseCommand.NotifyCanExecuteChanged();
            RefreshTreeCommand.NotifyCanExecuteChanged();
            AddFileCommand.NotifyCanExecuteChanged();
            CheckinFileCommand.NotifyCanExecuteChanged();
            CancelCommand.NotifyCanExecuteChanged();
        }
    }
}
