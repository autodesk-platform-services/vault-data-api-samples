using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using VaultDataAPISampleApp.Configuration;
using VaultDataAPISampleApp.Exceptions;
using VaultDataAPISampleApp.Http;
using VaultDataAPISampleApp.Models;
using VaultDataAPISampleApp.Services;

namespace VaultDataAPISampleApp.Services.Impl
{
    internal sealed class VaultDataApiService : IVaultDataApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly string _apiUrl;

        private string _serverAddress = string.Empty;
        private string? _accessToken;

        public bool HasAccessToken => !string.IsNullOrWhiteSpace(_accessToken);

        private Uri BaseUri
        {
            get
            {
                string serverAddress = _serverAddress.TrimEnd('/');
                return new Uri(serverAddress + _apiUrl, UriKind.Absolute);
            }
        }

        public VaultDataApiService(
            IOptions<VaultOptions> options,
            IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _apiUrl = options.Value.ApiBaseUri;
        }

        public void SetAccessToken(string accessToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
            _accessToken = accessToken;
        }

        public void ChangeServerAddress(string serverAddress)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(serverAddress);

            if (string.Equals(
                    _serverAddress,
                    serverAddress,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _serverAddress = serverAddress;
            _accessToken = null;
        }

        public async Task<bool> Login(string userName, string password)
        {
            var loginInput = new { input = new { vault = "Vault", userName = "administrator", password = "", appCode = "TC"  } };
            var json = JsonSerializer.Serialize(loginInput, JsonSerializerOptions.Web);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.PostAsync("sessions", content);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Login successful");
                var responseContent = await response.Content.ReadAsStringAsync();
                SessionResponse? loginResponse = JsonSerializer.Deserialize<SessionResponse>(responseContent, JsonSerializerOptions.Web);
                if (string.IsNullOrWhiteSpace(loginResponse?.Authorization))
                {
                    Console.WriteLine("The login response did not contain an access token.");
                    return false;
                }

                client.DefaultRequestHeaders.Add("Authorization", loginResponse.Authorization);
                return true;
            }
            else
            {
                Console.WriteLine("Login failed");
                return false;
            }
        }

        public async Task<PaginationResponse<UserResponse>?> GetUsersAsync()
        {
            using HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.GetAsync("users");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PaginationResponse<UserResponse>>(responseContent, JsonSerializerOptions.Web);
            }
            else
            {
                await ThrowVaultApiExceptionAsync(response);
                return null;
            }
        }

        public async Task<PaginationResponse<GroupResponse>?> GetGroupsAsync()
        {
            using HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.GetAsync("groups");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PaginationResponse<GroupResponse>>(responseContent, JsonSerializerOptions.Web);
            }
            else
            {
                await ThrowVaultApiExceptionAsync(response);
                return null;
            }
        }

        public async Task<PaginationResponse<FileVersionResponse>?> GetFilesAsync(string vaultId)
        {
            using HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.GetAsync($"vaults/{vaultId}/file-versions?limit=1000");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PaginationResponse<FileVersionResponse>>(responseContent, JsonSerializerOptions.Web);
            }
            else
            {
                await ThrowVaultApiExceptionAsync(response);
                return null;
            }
        }

        public async Task<VaultResponse?> GetVaultServerInfoAsync(string vaultId)
        {
            using HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.GetAsync($"vaults/{vaultId}");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<VaultResponse>(responseContent, JsonSerializerOptions.Web);
            }
            else
            {
                await ThrowVaultApiExceptionAsync(response);
                return null;
            }
        }

        public async Task<PaginationResponse<VaultResponse>?> GetVaultsAsync()
        {
            using HttpClient client = CreateClientWithoutAuth();
            using HttpResponseMessage response = await client.GetAsync("vaults");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PaginationResponse<VaultResponse>>(responseContent, JsonSerializerOptions.Web);
            }
            else
            {
                await ThrowVaultApiExceptionAsync(response);
                return null;
            }
        }

        public async Task<CursorPaginationResponse<ItemVersionResponse>?> GetItemVersionsAsync(
            string vaultId,
            int limit = 50)
        {
            using HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.GetAsync($"vaults/{vaultId}/item-versions?limit={limit}");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<CursorPaginationResponse<ItemVersionResponse>>(responseContent, JsonSerializerOptions.Web);
            }
            else
            {
                await ThrowVaultApiExceptionAsync(response);
                return null;
            }
        }

        public async Task<Dictionary<string, string>?> GetExtSyncConfigsAsync(string vaultId)
        {
            using HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.GetAsync($"vaults/{vaultId}/vault-options/ext-sync-configs");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Dictionary<string, string>>(responseContent, JsonSerializerOptions.Web);
            }
            else
            {
                await ThrowVaultApiExceptionAsync(response);
                return null;
            }
        }

        public async Task<ExtSyncTaskResponse?> CreateExtSyncTaskAsync(
            string vaultId,
            CreateExtSyncTaskRequest request)
        {
            var content = new StringContent(JsonSerializer.Serialize(request, JsonSerializerOptions.Web), Encoding.UTF8, "application/json");
            using HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.PostAsync($"vaults/{vaultId}/ext-sync-tasks", content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ExtSyncTaskResponse>(responseContent, JsonSerializerOptions.Web);
            }
            else
            {
                await ThrowVaultApiExceptionAsync(response);
                return null;
            }
        }

        public async Task<CursorPaginationResponse<ExtSyncTaskResponse>?> GetExtSyncTasksAsync(
            string vaultId,
            int limit = 10,
            string? cursorState = null)
        {
            var url = $"vaults/{vaultId}/ext-sync-tasks?limit={limit}";
            if (!string.IsNullOrEmpty(cursorState))
            {
                url += $"&cursorState={Uri.EscapeDataString(cursorState)}";
            }

            using HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<CursorPaginationResponse<ExtSyncTaskResponse>>(responseContent, JsonSerializerOptions.Web);
            }
            else
            {
                await ThrowVaultApiExceptionAsync(response);
                return null;
            }
        }

        public async Task<ExtSyncTaskResponse?> GetExtSyncTaskByIdAsync(
            string vaultId,
            string id)
        {
            using HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.GetAsync($"vaults/{vaultId}/ext-sync-tasks/{id}");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ExtSyncTaskResponse>(responseContent, JsonSerializerOptions.Web);
            }
            else
            {
                await ThrowVaultApiExceptionAsync(response);
                return null;
            }
        }

        public async Task<bool> DeleteExtSyncTaskAsync(string vaultId, string id)
        {
            using HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.DeleteAsync($"vaults/{vaultId}/ext-sync-tasks/{id}");
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                await ThrowVaultApiExceptionAsync(response);
                return false;
            }
        }

        public async Task<ExtSyncTaskResponse?> ResubmitExtSyncTaskAsync(
            string vaultId,
            string id)
        {
            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            using HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.PostAsync($"vaults/{vaultId}/ext-sync-tasks/{id}:resubmit", content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ExtSyncTaskResponse>(responseContent, JsonSerializerOptions.Web);
            }
            else
            {
                await ThrowVaultApiExceptionAsync(response);
                return null;
            }
        }

        public async Task<List<ExtSyncTaskResponse>?> FindExtSyncTasksByEntityIdsAsync(
            string vaultId,
            FindExtSyncTasksByEntityIdsRequest request)
        {
            var content = new StringContent(JsonSerializer.Serialize(request, JsonSerializerOptions.Web), Encoding.UTF8, "application/json");
            using HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.PostAsync($"vaults/{vaultId}/ext-sync-tasks:find-by-entity-ids", content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<ExtSyncTaskResponse>>(responseContent, JsonSerializerOptions.Web);
            }
            else
            {
                await ThrowVaultApiExceptionAsync(response);
                return null;
            }
        }

        public async Task<List<ExtSyncTaskResponse>?> BatchCreateExtSyncTasksAsync(
            string vaultId,
            List<CreateExtSyncTaskRequest> requests)
        {
            var content = new StringContent(JsonSerializer.Serialize(requests, JsonSerializerOptions.Web), Encoding.UTF8, "application/json");
            using HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.PostAsync($"vaults/{vaultId}/ext-sync-tasks:batch-create", content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<ExtSyncTaskResponse>>(responseContent, JsonSerializerOptions.Web);
            }
            else
            {
                await ThrowVaultApiExceptionAsync(response);
                return null;
            }
        }

        public async Task<CursorPaginationResponse<ExtSyncInfoResponse>?> GetItemVersionExtSyncInfosAsync(
            string vaultId,
            string itemVersionId,
            int limit = 10)
        {
            using HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.GetAsync($"vaults/{vaultId}/item-versions/{itemVersionId}/ext-sync-infos?limit={limit}");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<CursorPaginationResponse<ExtSyncInfoResponse>>(responseContent, JsonSerializerOptions.Web);
            }
            else
            {
                await ThrowVaultApiExceptionAsync(response);
                return null;
            }
        }

        public async Task<ExtSyncInfoResponse?> GetItemVersionExtSyncInfoAsync(
            string vaultId,
            string itemVersionId,
            string infoName)
        {
            using HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.GetAsync($"vaults/{vaultId}/item-versions/{itemVersionId}/ext-sync-infos/{Uri.EscapeDataString(infoName)}");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ExtSyncInfoResponse>(responseContent, JsonSerializerOptions.Web);
            }
            else
            {
                await ThrowVaultApiExceptionAsync(response);
                return null;
            }
        }

        public async Task<CursorPaginationResponse<ExtSyncInfoResponse>?> GetItemExtSyncInfosAsync(
            string vaultId,
            string itemId,
            int limit = 10)
        {
            using HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.GetAsync($"vaults/{vaultId}/items/{itemId}/ext-sync-infos?limit={limit}");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<CursorPaginationResponse<ExtSyncInfoResponse>>(responseContent, JsonSerializerOptions.Web);
            }
            else
            {
                await ThrowVaultApiExceptionAsync(response);
                return null;
            }
        }

        public async Task<ExtSyncInfoResponse?> GetItemExtSyncInfoAsync(
            string vaultId,
            string itemId,
            string infoName)
        {
            using HttpClient client = CreateClient();
            using HttpResponseMessage response = await client.GetAsync($"vaults/{vaultId}/items/{itemId}/ext-sync-infos/{Uri.EscapeDataString(infoName)}");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ExtSyncInfoResponse>(responseContent, JsonSerializerOptions.Web);
            }
            else
            {
                await ThrowVaultApiExceptionAsync(response);
                return null;
            }
        }

        private static async Task ThrowVaultApiExceptionAsync(HttpResponseMessage response)
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            string message = $"Vault API returned HTTP {(int)response.StatusCode}.";

            try
            {
                ErrorResponse? errorDetails = JsonSerializer.Deserialize<ErrorResponse>(
                    errorContent,
                    JsonSerializerOptions.Web);
                if (errorDetails != null)
                {
                    message = $"Vault API returned HTTP {(int)response.StatusCode}: "
                        + $"{errorDetails.ErrorCode}. {errorDetails.Detail}";
                }
            }
            catch (JsonException)
            {
                if (!string.IsNullOrWhiteSpace(errorContent))
                {
                    const int maxErrorLength = 500;
                    string errorSummary = errorContent.Length <= maxErrorLength
                        ? errorContent
                        : errorContent[..maxErrorLength] + "...";
                    message += $" {errorSummary}";
                }
            }

            throw new VaultApiException(response.StatusCode, message);
        }

        private HttpClient CreateClient()
        {
            HttpClient client =
                _httpClientFactory.CreateClient(VaultApiHttpClient.Name);
            client.BaseAddress = BaseUri;

            if (!string.IsNullOrWhiteSpace(_accessToken))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _accessToken);
            }

            return client;
        }

        private HttpClient CreateClientWithoutAuth()
        {
            HttpClient client =
                _httpClientFactory.CreateClient(VaultApiHttpClient.Name);
            client.BaseAddress = BaseUri;

            return client;
        }

    }
}
