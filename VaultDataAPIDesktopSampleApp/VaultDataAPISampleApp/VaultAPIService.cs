using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using VaultDataAPISampleApp.Models;


namespace VaultDataAPISampleApp
{
    public class VaultAPIService
    {
        private static VaultAPIService instance = null;
        private static readonly object padlock = new object();
        private VaultResponse vaultServer;
        private string serverAddress;
        private string apiUrl = ConfigurationManager.AppSettings["ApiBaseUri"];
        private string accessToken;
        private string clientId;
        private HttpClient client;
        private bool disposed = false;

        private VaultAPIService(string address)
        {
            serverAddress = address;
            client = new HttpClient();
            client.BaseAddress = new Uri(GetBaseUrl());
        }

        public static void Initialize(string address)
        {
            if (instance == null)
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new VaultAPIService(address);
                    }
                }
            }
        }

        public void UpdateBaseAddress()
        {
            client.BaseAddress = new Uri(GetBaseUrl());
        }

        public static VaultAPIService Instance
        {
            get
            {
                return instance;
            }
        }

        public void SetClientId(string id)
        {
            clientId = id;
        }

        public string GetClientId()
        {
            return clientId;
        }

        public void SetAccessToken(string token)
        {
            accessToken = token;
            SetToken(token);
        }

        public string GetAccessToken()
        {
            return accessToken;
        }

        public string GetServerAddress()
        {
            return serverAddress;
        }

        public void SetServerAddress(string server)
        {
            serverAddress = server;
            UpdateBaseAddress();
        }

        public string GetBaseUrl()
        {
            var processedServerAddress = serverAddress.EndsWith("/") ? serverAddress.TrimEnd('/') : serverAddress;
            return processedServerAddress + apiUrl;
        }

        public void SetVaultServer(VaultResponse vaultResponse)
        {
            vaultServer = vaultResponse;
        }

        public async Task<bool> Login(string userName, string password)
        {
            var loginInput = new { input = new { vault = "Vault", userName = "administrator", password = "", appCode = "TC"  } };
            var json = JsonConvert.SerializeObject(loginInput);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync("sessions", content);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Login successful");
                var responseContent = await response.Content.ReadAsStringAsync();
                SessionResponse loginResponse = JsonConvert.DeserializeObject<SessionResponse>(responseContent);
                client.DefaultRequestHeaders.Add("Authorization", loginResponse.Authorization);
                return true;
            }
            else
            {
                Console.WriteLine("Login failed");
                return false;
            }
        }

        public void SetToken(string token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                if (client.DefaultRequestHeaders.Contains("Authorization"))
                {
                    client.DefaultRequestHeaders.Remove("Authorization");
                }
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            }
        }


        public async Task<PaginationResponse<UserResponse>> GetUsersAsync()
        {
            HttpResponseMessage response = await client.GetAsync("users");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<PaginationResponse<UserResponse>>(responseContent);
            }
            else
            {
                await ShowErrorDialogAsync(response);
                return null;
            }
        }

        public async Task<PaginationResponse<GroupResponse>> GetGroupsAsync()
        {
            HttpResponseMessage response = await client.GetAsync("groups");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<PaginationResponse<GroupResponse>>(responseContent);
            }
            else
            {
                await ShowErrorDialogAsync(response);
                return null;
            }
        }

        public async Task<PaginationResponse<FileVersionResponse>> GetFilesAsync()
        {
            HttpResponseMessage response = await client.GetAsync($"vaults/{vaultServer.Id}/file-versions?limit=1000");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<PaginationResponse<FileVersionResponse>>(responseContent);
            }
            else
            {
                await ShowErrorDialogAsync(response);
                return null;
            }
        }

        public async Task<VaultResponse> GetVaultServerInfoAsync()
        {
            HttpResponseMessage response = await client.GetAsync($"vaults/{vaultServer.Id}");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<VaultResponse>(responseContent);
            }
            else
            {
                await ShowErrorDialogAsync(response);
                return null;
            }
        }

        public async Task<PaginationResponse<VaultResponse>> GetVaultsAsync()
        {
            HttpResponseMessage response = await client.GetAsync("vaults");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<PaginationResponse<VaultResponse>>(responseContent);
            }
            else
            {
                await ShowErrorDialogAsync(response);
                return null;
            }
        }

        public async Task<CursorPaginationResponse<ItemVersionResponse>> GetItemVersionsAsync(int limit = 50)
        {
            if (vaultServer == null) return null;
            HttpResponseMessage response = await client.GetAsync($"vaults/{vaultServer.Id}/item-versions?limit={limit}");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<CursorPaginationResponse<ItemVersionResponse>>(responseContent);
            }
            else
            {
                await ShowErrorDialogAsync(response);
                return null;
            }
        }

        public async Task<Dictionary<string, string>> GetExtSyncConfigsAsync()
        {
            if (vaultServer == null) return null;
            HttpResponseMessage response = await client.GetAsync($"vaults/{vaultServer.Id}/vault-options/ext-sync-configs");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);
            }
            else
            {
                await ShowErrorDialogAsync(response);
                return null;
            }
        }

        public async Task<ExtSyncTaskResponse> CreateExtSyncTaskAsync(CreateExtSyncTaskRequest request)
        {
            if (vaultServer == null) return null;
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync($"vaults/{vaultServer.Id}/ext-sync-tasks", content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ExtSyncTaskResponse>(responseContent);
            }
            else
            {
                await ShowErrorDialogAsync(response);
                return null;
            }
        }

        public async Task<CursorPaginationResponse<ExtSyncTaskResponse>> GetExtSyncTasksAsync(int limit = 10, string cursorState = null)
        {
            if (vaultServer == null) return null;
            var url = $"vaults/{vaultServer.Id}/ext-sync-tasks?limit={limit}";
            if (!string.IsNullOrEmpty(cursorState))
                url += $"&cursorState={Uri.EscapeDataString(cursorState)}";
            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<CursorPaginationResponse<ExtSyncTaskResponse>>(responseContent);
            }
            else
            {
                await ShowErrorDialogAsync(response);
                return null;
            }
        }

        public async Task<ExtSyncTaskResponse> GetExtSyncTaskByIdAsync(string id)
        {
            if (vaultServer == null) return null;
            HttpResponseMessage response = await client.GetAsync($"vaults/{vaultServer.Id}/ext-sync-tasks/{id}");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ExtSyncTaskResponse>(responseContent);
            }
            else
            {
                await ShowErrorDialogAsync(response);
                return null;
            }
        }

        public async Task<bool> DeleteExtSyncTaskAsync(string id)
        {
            if (vaultServer == null) return false;
            HttpResponseMessage response = await client.DeleteAsync($"vaults/{vaultServer.Id}/ext-sync-tasks/{id}");
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                await ShowErrorDialogAsync(response);
                return false;
            }
        }

        public async Task<ExtSyncTaskResponse> ResubmitExtSyncTaskAsync(string id)
        {
            if (vaultServer == null) return null;
            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync($"vaults/{vaultServer.Id}/ext-sync-tasks/{id}:resubmit", content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ExtSyncTaskResponse>(responseContent);
            }
            else
            {
                await ShowErrorDialogAsync(response);
                return null;
            }
        }

        public async Task<List<ExtSyncTaskResponse>> FindExtSyncTasksByEntityIdsAsync(FindExtSyncTasksByEntityIdsRequest request)
        {
            if (vaultServer == null) return null;
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync($"vaults/{vaultServer.Id}/ext-sync-tasks:find-by-entity-ids", content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<ExtSyncTaskResponse>>(responseContent);
            }
            else
            {
                await ShowErrorDialogAsync(response);
                return null;
            }
        }

        public async Task<List<ExtSyncTaskResponse>> BatchCreateExtSyncTasksAsync(List<CreateExtSyncTaskRequest> requests)
        {
            if (vaultServer == null) return null;
            var content = new StringContent(JsonConvert.SerializeObject(requests), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync($"vaults/{vaultServer.Id}/ext-sync-tasks:batch-create", content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<ExtSyncTaskResponse>>(responseContent);
            }
            else
            {
                await ShowErrorDialogAsync(response);
                return null;
            }
        }

        public async Task<CursorPaginationResponse<ExtSyncInfoResponse>> GetItemVersionExtSyncInfosAsync(string itemVersionId, int limit = 10)
        {
            if (vaultServer == null) return null;
            HttpResponseMessage response = await client.GetAsync($"vaults/{vaultServer.Id}/item-versions/{itemVersionId}/ext-sync-infos?limit={limit}");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<CursorPaginationResponse<ExtSyncInfoResponse>>(responseContent);
            }
            else
            {
                await ShowErrorDialogAsync(response);
                return null;
            }
        }

        public async Task<ExtSyncInfoResponse> GetItemVersionExtSyncInfoAsync(string itemVersionId, string infoName)
        {
            if (vaultServer == null) return null;
            HttpResponseMessage response = await client.GetAsync($"vaults/{vaultServer.Id}/item-versions/{itemVersionId}/ext-sync-infos/{Uri.EscapeDataString(infoName)}");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ExtSyncInfoResponse>(responseContent);
            }
            else
            {
                await ShowErrorDialogAsync(response);
                return null;
            }
        }

        public async Task<CursorPaginationResponse<ExtSyncInfoResponse>> GetItemExtSyncInfosAsync(string itemId, int limit = 10)
        {
            if (vaultServer == null) return null;
            HttpResponseMessage response = await client.GetAsync($"vaults/{vaultServer.Id}/items/{itemId}/ext-sync-infos?limit={limit}");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<CursorPaginationResponse<ExtSyncInfoResponse>>(responseContent);
            }
            else
            {
                await ShowErrorDialogAsync(response);
                return null;
            }
        }

        public async Task<ExtSyncInfoResponse> GetItemExtSyncInfoAsync(string itemId, string infoName)
        {
            if (vaultServer == null) return null;
            HttpResponseMessage response = await client.GetAsync($"vaults/{vaultServer.Id}/items/{itemId}/ext-sync-infos/{Uri.EscapeDataString(infoName)}");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ExtSyncInfoResponse>(responseContent);
            }
            else
            {
                await ShowErrorDialogAsync(response);
                return null;
            }
        }

        public async Task ShowErrorDialogAsync(HttpResponseMessage response)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            var errorDetails = JsonConvert.DeserializeObject<ErrorResponse>(errorContent);
            MessageBox.Show($"Status code: {errorDetails.StatusCode}. Error Code {errorDetails.ErrorCode}. Error Detail: {errorDetails.Detail}");
        }

        public static void ResetInstance()
        {
            lock (padlock)
            {
                if (instance != null)
                {
                    instance.Dispose();
                    instance = null;
                }
            }
        }

        // Implement IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    client?.Dispose();
                }
                disposed = true;
            }
        }

        ~VaultAPIService()
        {
            Dispose(false);
        }


    }
}
