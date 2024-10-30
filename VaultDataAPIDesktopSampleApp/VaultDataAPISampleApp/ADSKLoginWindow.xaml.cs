using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Security.Cryptography;
using System.Net.Http;
using Newtonsoft.Json;
using System.Configuration;
using Microsoft.Web.WebView2.Core;
using VaultDataAPISampleApp.Models;

namespace VaultDataAPISampleApp
{
    /// <summary>
    /// Interaction logic for ADSKLoginWindow.xaml
    /// </summary>
    public partial class ADSKLoginWindow : Window
    {
        private string clientId;

        private string codeVerifier;
        private string codeChallenge; 
        private string auth_code;
        private string redirectUri = ConfigurationManager.AppSettings["RedirectUri"];

        private string getAccessTokenUrl = ConfigurationManager.AppSettings["GetAccessTokenUrl"];
        private string loginUrl = ConfigurationManager.AppSettings["LoginUrl"];

        public ADSKLoginWindow(string clientId)
        {
            InitializeComponent();
            codeVerifier = GenerateRandomString();
            CalculateCodeChallenge();
            this.clientId = clientId;

            webBrowser.NavigationStarting += WebBrowser_Navigating;
            //webBrowser.NavigationCompleted += WebBrowser_Navigated;

            webBrowser.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;

            InitializeAsync();
        }

        private void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                // Initialization succeeded, now you can use the CoreWebView2.
            }
            else
            {
                // Initialization failed, check `e.InitializationException` for more details.
            }
        }


        private async void InitializeAsync()
        {
            await webBrowser.EnsureCoreWebView2Async();

            string responseType = "response_type=code";
            string clientIdString = $"client_id={clientId}";
            string redirectUriString = $"redirect_uri={redirectUri}";
            string codeChallengeString = $"code_challenge={codeChallenge}";
            string codeChallengeMethod = "code_challenge_method=S256";
            string loginUrlString = $"{loginUrl}?{responseType}&{clientIdString}&{redirectUriString}&nonce=1232132&scope=data:read&prompt=login&state=12321321&{codeChallengeString}&{codeChallengeMethod}";
            webBrowser.CoreWebView2.Navigate(loginUrlString);
        }

        private void WebBrowser_Navigating(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            var url = e.Uri;

            // If the URL is the redirect URL, parse the token from the URL.
            if (url.StartsWith(redirectUri))
            {
                //get code parameter from redirect uri  
                var uri = new Uri(e.Uri.ToString());
                auth_code = HttpUtility.ParseQueryString(uri.Query).Get("code");

                var token = GetAccessToken(auth_code);
                VaultAPIService.Instance.SetAccessToken(token);

                this.DialogResult = true;
            }
        }

        private string GetAccessToken(string auth_code)
        {
            // Post request to get access token
            var client = new HttpClient()
            {
                BaseAddress = new Uri(getAccessTokenUrl)
            };

            // Example of a curl request
            //curl -v 'https://developer.api.autodesk.com/authentication/v2/token'
            //-X 'POST'
            //- H 'Content-Type: application/x-www-form-urlencoded''
            //- H 'accept: application/json' \'
            //- d 'grant_type=authorization_code'
            //- d 'client_id=GCi5oTYLE36CTUlcL7wWbhq9mC5DzG9w'
            //- d 'code_verifier=ZGI6X4QR3FFXh3Bs9zNMgazTYDHEb_GqTt_fue4tFKYjRNR9N32bCqr~Hsxl673Ssf0RqyxC0avKNo_AKlE_7tj6cm4i5XbmjuGrCsu7X9rE~MqmoBLrLjvmvQscCfi2'
            //- d 'code=wroM1vFA4E-Aj241-quh_LVjm7UldawnNgYEHQ8I'
            //- d 'redirect_uri=http://localhost:8080/oauth/callback/'

            client.DefaultRequestHeaders.Add("accept", "application/json");
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("code_verifier", codeVerifier),
                new KeyValuePair<string, string>("code", auth_code),
                new KeyValuePair<string, string>("redirect_uri", redirectUri)
            });
            var response = client.PostAsync(getAccessTokenUrl, content).Result;
            if (response.IsSuccessStatusCode)
            {
                var responseContent = response.Content.ReadAsStringAsync().Result;
                //get access token from response
                var token = JsonConvert.DeserializeObject<AccessTokenResponse>(responseContent);
                return token.access_token;
            }

            return null;
        }

        private void CalculateCodeChallenge()
        {
            using (var sha256 = SHA256.Create())
            {
                // Here we create a hash of the code verifier
                var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));

                // and produce the "Code Challenge" from it by base64Url encoding it.
                string base64 = Convert.ToBase64String(challengeBytes);
                codeChallenge = base64.TrimEnd('=').Replace('+', '-').Replace('/', '_'); 
            }
        }

        private string GenerateRandomString()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";
            var stringChars = new char[new Random().Next(43, 129)];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            return new String(stringChars);
        }
    }
}
