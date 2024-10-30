using System;
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


        public async Task<PaginationResponse<UserResponse>> GetUsers()
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

        public async Task<PaginationResponse<GroupResponse>> GetGroups()
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

        public async Task<PaginationResponse<FileVersionResponse>> GetFiles()
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

        public async Task<VaultResponse> GetVaultServerInfo()
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

        public async Task<PaginationResponse<VaultResponse>> GetVaults()
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
