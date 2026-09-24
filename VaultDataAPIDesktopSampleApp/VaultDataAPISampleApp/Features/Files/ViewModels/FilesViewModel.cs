using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;

using SkiaSharp;

using VaultDataAPISampleApp.Dialogs;
using VaultDataAPISampleApp.Models;
using VaultDataAPISampleApp.Services;
using VaultDataAPISampleApp.State;

namespace VaultDataAPISampleApp.Features.Files.ViewModels
{
    public sealed class FilesViewModel : ObservableObject, IDisposable
    {
        private readonly IVaultDataApiService _vaultApiService;
        private readonly VaultSession _session;
        private readonly IMessageDialogService _messageDialogService;

        private int _totalCount;
        private ISeries[] _fileTypeSeries = CreateEmptySeries();

        public FilesViewModel(
            IVaultDataApiService vaultApiService,
            VaultSession session,
            IMessageDialogService messageDialogService)
        {
            _vaultApiService = vaultApiService;
            _session = session;
            _messageDialogService = messageDialogService;

            LoadFilesCommand = new AsyncRelayCommand(LoadFilesAsync, CanLoadFiles);
            _session.PropertyChanged += SessionPropertyChanged;
        }

        public ObservableCollection<FileVersionResponse> Files { get; } = [];

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

        public ISeries[] FileTypeSeries
        {
            get
            {
                return _fileTypeSeries;
            }
            private set
            {
                SetProperty(ref _fileTypeSeries, value);
            }
        }

        public bool IsEmpty => Files.Count == 0;

        public IAsyncRelayCommand LoadFilesCommand { get; }

        public void Dispose()
        {
            _session.PropertyChanged -= SessionPropertyChanged;
        }

        private bool CanLoadFiles()
        {
            return _session.IsAuthenticated
                && !string.IsNullOrWhiteSpace(_session.SelectedVaultId);
        }

        private async Task LoadFilesAsync()
        {
            string? vaultId = _session.SelectedVaultId;
            if (string.IsNullOrWhiteSpace(vaultId))
            {
                _messageDialogService.ShowInformation(
                    "Please select a vault first");
                return;
            }

            try
            {
                PaginationResponse<FileVersionResponse>? result =
                    await _vaultApiService.GetFilesAsync(vaultId);

                if (!string.Equals(
                        _session.SelectedVaultId,
                        vaultId,
                        StringComparison.Ordinal))
                {
                    return;
                }

                if (result?.Pagination == null || result.Results == null)
                {
                    return;
                }

                Files.Clear();
                foreach (FileVersionResponse file in result.Results)
                {
                    Files.Add(file);
                }

                TotalCount = result.Pagination.TotalResults;
                FileTypeSeries = CreateFileTypeSeries(Files);
                OnPropertyChanged(nameof(IsEmpty));
            }
            catch (Exception exception)
            {
                _messageDialogService.ShowError(exception.Message);
            }
        }

        private void SessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VaultSession.SelectedVaultId))
            {
                Reset();
            }

            if (e.PropertyName == nameof(VaultSession.SelectedVaultId)
                || e.PropertyName == nameof(VaultSession.IsAuthenticated))
            {
                LoadFilesCommand.NotifyCanExecuteChanged();
            }
        }

        private void Reset()
        {
            Files.Clear();
            TotalCount = 0;
            FileTypeSeries = CreateEmptySeries();
            OnPropertyChanged(nameof(IsEmpty));
        }

        private static ISeries[] CreateFileTypeSeries(
            ObservableCollection<FileVersionResponse> files)
        {
            int iptFileCount = files.Count(file =>
                file.Name.Contains(".ipt", StringComparison.OrdinalIgnoreCase));
            int iamFileCount = files.Count(file =>
                file.Name.Contains(".iam", StringComparison.OrdinalIgnoreCase));
            int dwgFileCount = files.Count(file =>
                file.Name.Contains(".dwg", StringComparison.OrdinalIgnoreCase));
            int dwfFileCount = files.Count(file =>
                file.Name.Contains(".dwf", StringComparison.OrdinalIgnoreCase));
            int otherFileCount = files.Count
                - (iptFileCount + iamFileCount + dwgFileCount + dwfFileCount);

            return
            [
                CreateSeries("Ipt file", iptFileCount, new SKColor(0xE0, 0xAF, 0x4B)),
                CreateSeries("Iam file", iamFileCount, new SKColor(0xE1, 0xE1, 0x54)),
                CreateSeries("Dwg file", dwgFileCount, new SKColor(0x68, 0x9E, 0xD4)),
                CreateSeries("Dwf file", dwfFileCount, new SKColor(0x9C, 0x6B, 0xCE)),
                CreateSeries("Other file", otherFileCount, new SKColor(0xB2, 0xB2, 0xB5))
            ];
        }

        private static ISeries[] CreateEmptySeries()
        {
            return [];
        }

        private static PieSeries<double> CreateSeries(
            string name,
            double value,
            SKColor color)
        {
            return new PieSeries<double>
            {
                Name = name,
                Values = [value],
                Fill = new SolidColorPaint(color),
                DataLabelsPaint = new SolidColorPaint(SKColors.Black)
            };
        }
    }
}
