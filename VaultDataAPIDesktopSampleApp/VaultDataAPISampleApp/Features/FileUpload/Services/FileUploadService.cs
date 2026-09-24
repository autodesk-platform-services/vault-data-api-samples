using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using VaultDataAPISampleApp.Models;
using VaultDataAPISampleApp.Services;

namespace VaultDataAPISampleApp.Features.FileUpload.Services
{
    internal sealed class FileUploadService : IFileUploadService
    {
        private const int s_preferredPartSize = 8 * 1024 * 1024;

        private readonly IVaultDataApiService _vaultApiService;

        public FileUploadService(IVaultDataApiService vaultApiService)
        {
            _vaultApiService = vaultApiService;
        }

        public async Task<FileResponse> AddFileAsync(
            string vaultId,
            long folderId,
            string localFilePath,
            string? comment,
            IProgress<FileUploadProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            string uploadTicket = await UploadAsync(
                vaultId,
                localFilePath,
                progress,
                cancellationToken);

            progress?.Report(new FileUploadProgress("Adding file to Vault..."));
            return await _vaultApiService.AddFileAsync(
                vaultId,
                new AddFileRequest
                {
                    FolderId = folderId,
                    UploadTicket = uploadTicket,
                    Name = Path.GetFileName(localFilePath),
                    Comment = NullIfWhiteSpace(comment)
                },
                cancellationToken);
        }

        public async Task<FileVersionResponse> CheckinFileAsync(
            string vaultId,
            string fileId,
            string localFilePath,
            string? comment,
            bool checkoutFirst,
            bool keepCheckedOut,
            IProgress<FileUploadProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            ValidateLocalFile(localFilePath);

            if (checkoutFirst)
            {
                progress?.Report(new FileUploadProgress("Checking out file..."));
                await _vaultApiService.CheckoutFileAsync(
                    vaultId,
                    fileId,
                    cancellationToken);
            }

            string uploadTicket = await UploadAsync(
                vaultId,
                localFilePath,
                progress,
                cancellationToken);

            progress?.Report(new FileUploadProgress("Checking in file..."));
            return await _vaultApiService.CheckinFileAsync(
                vaultId,
                fileId,
                new CheckinFileRequest
                {
                    UploadTicket = uploadTicket,
                    Comment = NullIfWhiteSpace(comment),
                    KeepCheckedOut = keepCheckedOut
                },
                cancellationToken);
        }

        private async Task<string> UploadAsync(
            string vaultId,
            string localFilePath,
            IProgress<FileUploadProgress>? progress,
            CancellationToken cancellationToken)
        {
            ValidateLocalFile(localFilePath);

            var fileInfo = new FileInfo(localFilePath);
            progress?.Report(new FileUploadProgress("Creating upload session..."));
            FileUploadSessionResponse upload =
                await _vaultApiService.CreateFileUploadAsync(
                    vaultId,
                    fileInfo.Name,
                    cancellationToken);
            ValidateInitialUploadSession(upload);

            string uploadId = upload.UploadId;
            string uploadSessionToken = upload.UploadSessionToken;

            int partSize = (int)Math.Min(upload.MaxPartSize, s_preferredPartSize);
            byte[] buffer = new byte[partSize];
            long bytesTransferred = 0;
            int partIndex = 0;

            await using var stream = new FileStream(
                localFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                64 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            if (fileInfo.Length == 0)
            {
                progress?.Report(new FileUploadProgress(
                    "Uploading empty file...",
                    0,
                    0));
                FileUploadSessionResponse emptyPartResult =
                    await _vaultApiService.UploadFileContentPartAsync(
                        vaultId,
                        uploadId,
                        0,
                        uploadSessionToken,
                        buffer,
                        0,
                        cancellationToken);
                if (string.IsNullOrWhiteSpace(
                        emptyPartResult.UploadSessionToken))
                {
                    throw new InvalidOperationException(
                        "Vault did not return the continuation token for the upload.");
                }

                uploadSessionToken = emptyPartResult.UploadSessionToken;
            }

            while (true)
            {
                int count = await ReadPartAsync(
                    stream,
                    buffer,
                    cancellationToken);
                if (count == 0)
                {
                    break;
                }

                progress?.Report(new FileUploadProgress(
                    $"Uploading part {partIndex + 1}...",
                    bytesTransferred,
                    fileInfo.Length));

                FileUploadSessionResponse partResult =
                    await _vaultApiService.UploadFileContentPartAsync(
                        vaultId,
                        uploadId,
                        partIndex,
                        uploadSessionToken,
                        buffer,
                        count,
                        cancellationToken);
                if (string.IsNullOrWhiteSpace(partResult.UploadSessionToken))
                {
                    throw new InvalidOperationException(
                        "Vault did not return the continuation token for the next upload part.");
                }

                uploadSessionToken = partResult.UploadSessionToken;

                bytesTransferred += count;
                partIndex++;
                progress?.Report(new FileUploadProgress(
                    $"Uploaded {bytesTransferred:N0} of {fileInfo.Length:N0} bytes",
                    bytesTransferred,
                    fileInfo.Length));
            }

            progress?.Report(new FileUploadProgress("Completing upload..."));
            FileUploadCompletionResponse completion =
                await _vaultApiService.CompleteFileUploadAsync(
                    vaultId,
                    uploadId,
                    uploadSessionToken,
                    cancellationToken);

            if (string.IsNullOrWhiteSpace(completion.UploadTicket))
            {
                throw new InvalidOperationException(
                    "Vault completed the upload without returning an upload ticket.");
            }

            return completion.UploadTicket;
        }

        private static void ValidateInitialUploadSession(
            FileUploadSessionResponse upload)
        {
            if (string.IsNullOrWhiteSpace(upload.UploadId)
                || string.IsNullOrWhiteSpace(upload.UploadSessionToken)
                || upload.MaxPartSize <= 0)
            {
                throw new InvalidOperationException(
                    "Vault returned an invalid upload session.");
            }
        }

        private static async Task<int> ReadPartAsync(
            Stream stream,
            byte[] buffer,
            CancellationToken cancellationToken)
        {
            int totalRead = 0;
            while (totalRead < buffer.Length)
            {
                int count = await stream.ReadAsync(
                    buffer.AsMemory(totalRead, buffer.Length - totalRead),
                    cancellationToken);
                if (count == 0)
                {
                    break;
                }

                totalRead += count;
            }

            return totalRead;
        }

        private static void ValidateLocalFile(string localFilePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localFilePath);
            if (!File.Exists(localFilePath))
            {
                throw new FileNotFoundException(
                    "The selected local file no longer exists.",
                    localFilePath);
            }
        }

        private static string? NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
