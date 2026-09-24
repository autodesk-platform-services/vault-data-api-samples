using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using VaultDataAPISampleApp.Models;

namespace VaultDataAPISampleApp.Services
{
    public interface IVaultDataApiService
    {
        bool HasAccessToken { get; }

        void SetAccessToken(string accessToken);

        void ChangeServerAddress(string serverAddress);

        Task<bool> Login(string userName, string password);

        Task<PaginationResponse<UserResponse>?> GetUsersAsync();

        Task<PaginationResponse<GroupResponse>?> GetGroupsAsync();

        Task<PaginationResponse<FileVersionResponse>?> GetFilesAsync(string vaultId);

        Task<FileVersionResponse> GetFileVersionAsync(
            string vaultId,
            string fileVersionId,
            CancellationToken cancellationToken = default);

        Task<FolderResponse> GetFolderAsync(
            string vaultId,
            string folderId,
            CancellationToken cancellationToken = default);

        Task<FolderContentsResponse> GetFolderContentsAsync(
            string vaultId,
            string folderId,
            CancellationToken cancellationToken = default);

        Task<FileVersionResponse> CheckoutFileAsync(
            string vaultId,
            string fileId,
            CancellationToken cancellationToken = default);

        Task<FileUploadSessionResponse> CreateFileUploadAsync(
            string vaultId,
            string fileName,
            CancellationToken cancellationToken = default);

        Task<FileUploadSessionResponse> UploadFileContentPartAsync(
            string vaultId,
            string uploadId,
            int partIndex,
            string uploadSessionToken,
            byte[] buffer,
            int count,
            CancellationToken cancellationToken = default);

        Task<FileUploadCompletionResponse> CompleteFileUploadAsync(
            string vaultId,
            string uploadId,
            string uploadSessionToken,
            CancellationToken cancellationToken = default);

        Task<FileResponse> AddFileAsync(
            string vaultId,
            AddFileRequest request,
            CancellationToken cancellationToken = default);

        Task<FileVersionResponse> CheckinFileAsync(
            string vaultId,
            string fileId,
            CheckinFileRequest request,
            CancellationToken cancellationToken = default);

        Task<VaultResponse?> GetVaultServerInfoAsync(string vaultId);

        Task<PaginationResponse<VaultResponse>?> GetVaultsAsync();

        Task<CursorPaginationResponse<ItemVersionResponse>?> GetItemVersionsAsync(
            string vaultId,
            int limit = 50);

        Task<Dictionary<string, string>?> GetExtSyncConfigsAsync(string vaultId);

        Task<ExtSyncTaskResponse?> CreateExtSyncTaskAsync(
            string vaultId,
            CreateExtSyncTaskRequest request);

        Task<CursorPaginationResponse<ExtSyncTaskResponse>?> GetExtSyncTasksAsync(
            string vaultId,
            int limit = 10,
            string? cursorState = null);

        Task<ExtSyncTaskResponse?> GetExtSyncTaskByIdAsync(
            string vaultId,
            string id);

        Task<bool> DeleteExtSyncTaskAsync(string vaultId, string id);

        Task<ExtSyncTaskResponse?> ResubmitExtSyncTaskAsync(
            string vaultId,
            string id);

        Task<List<ExtSyncTaskResponse>?> FindExtSyncTasksByEntityIdsAsync(
            string vaultId,
            FindExtSyncTasksByEntityIdsRequest request);

        Task<List<ExtSyncTaskResponse>?> BatchCreateExtSyncTasksAsync(
            string vaultId,
            List<CreateExtSyncTaskRequest> requests);

        Task<CursorPaginationResponse<ExtSyncInfoResponse>?> GetItemVersionExtSyncInfosAsync(
            string vaultId,
            string itemVersionId,
            int limit = 10);

        Task<ExtSyncInfoResponse?> GetItemVersionExtSyncInfoAsync(
            string vaultId,
            string itemVersionId,
            string infoName);

        Task<CursorPaginationResponse<ExtSyncInfoResponse>?> GetItemExtSyncInfosAsync(
            string vaultId,
            string itemId,
            int limit = 10);

        Task<ExtSyncInfoResponse?> GetItemExtSyncInfoAsync(
            string vaultId,
            string itemId,
            string infoName);
    }
}
