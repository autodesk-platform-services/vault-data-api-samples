using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

using VaultDataAPISampleApp.Models;

namespace VaultDataAPISampleApp.Features.FileUpload.ViewModels
{
    public sealed class VaultContentNode : ObservableObject
    {
        private readonly Func<VaultContentNode, Task<IReadOnlyList<VaultContentNode>>>?
            _childrenLoader;
        private readonly Action<Exception>? _loadFailed;
        private readonly Action<VaultContentNode>? _selected;

        private bool _isExpanded;
        private bool _isSelected;
        private bool _isLoading;
        private bool _childrenLoaded;

        private VaultContentNode(
            string displayName,
            Action<VaultContentNode>? selected)
        {
            DisplayName = displayName;
            IsPlaceholder = true;
            _selected = selected;
        }

        private VaultContentNode(
            FolderResponse folder,
            Func<VaultContentNode, Task<IReadOnlyList<VaultContentNode>>>
                childrenLoader,
            Action<Exception> loadFailed,
            Action<VaultContentNode> selected)
        {
            Folder = folder;
            DisplayName = folder.Name ?? folder.FullName ?? "Folder";
            Detail = folder.FullName;
            _childrenLoader = childrenLoader;
            _loadFailed = loadFailed;
            _selected = selected;
            Children.Add(new VaultContentNode("Expand to load...", selected));
        }

        private VaultContentNode(
            FileVersionResponse file,
            Action<VaultContentNode> selected)
        {
            File = file;
            DisplayName = file.Name;
            Detail = $"Version {file.Version}";
            _selected = selected;
        }

        public ObservableCollection<VaultContentNode> Children { get; } = [];

        public FolderResponse? Folder { get; }

        public FileVersionResponse? File { get; }

        public string DisplayName { get; }

        public string? Detail { get; }

        public bool IsFolder => Folder != null;

        public bool IsPlaceholder { get; }

        public bool IsExpanded
        {
            get
            {
                return _isExpanded;
            }
            set
            {
                if (SetProperty(ref _isExpanded, value)
                    && value
                    && IsFolder)
                {
                    _ = EnsureChildrenLoadedAsync();
                }
            }
        }

        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                if (SetProperty(ref _isSelected, value) && value)
                {
                    _selected?.Invoke(this);
                }
            }
        }

        public bool IsLoading
        {
            get
            {
                return _isLoading;
            }
            private set
            {
                SetProperty(ref _isLoading, value);
            }
        }

        public static VaultContentNode CreateFolder(
            FolderResponse folder,
            Func<VaultContentNode, Task<IReadOnlyList<VaultContentNode>>>
                childrenLoader,
            Action<Exception> loadFailed,
            Action<VaultContentNode> selected)
        {
            return new VaultContentNode(
                folder,
                childrenLoader,
                loadFailed,
                selected);
        }

        public static VaultContentNode CreateFile(
            FileVersionResponse file,
            Action<VaultContentNode> selected)
        {
            return new VaultContentNode(file, selected);
        }

        private async Task EnsureChildrenLoadedAsync()
        {
            if (_childrenLoaded || IsLoading || _childrenLoader == null)
            {
                return;
            }

            IsLoading = true;
            try
            {
                IReadOnlyList<VaultContentNode> children =
                    await _childrenLoader(this);
                Children.Clear();
                foreach (VaultContentNode child in children)
                {
                    Children.Add(child);
                }

                _childrenLoaded = true;
            }
            catch (Exception exception)
            {
                Children.Clear();
                Children.Add(new VaultContentNode(
                    "Could not load. Collapse and expand to retry.",
                    _selected));
                _loadFailed?.Invoke(exception);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
