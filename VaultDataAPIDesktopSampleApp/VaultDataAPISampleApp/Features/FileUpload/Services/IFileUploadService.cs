using System;
using System.Threading;
using System.Threading.Tasks;

using VaultDataAPISampleApp.Models;

namespace VaultDataAPISampleApp.Features.FileUpload.Services
{
    public interface IFileUploadService
    {
        Task<FileResponse> AddFileAsync(
            string vaultId,
            long folderId,
            string localFilePath,
            string? comment,
            IProgress<FileUploadProgress>? progress = null,
            CancellationToken cancellationToken = default);

        Task<FileVersionResponse> CheckinFileAsync(
            string vaultId,
            string fileId,
            string localFilePath,
            string? comment,
            bool checkoutFirst,
            bool keepCheckedOut,
            IProgress<FileUploadProgress>? progress = null,
            CancellationToken cancellationToken = default);
    }
}
