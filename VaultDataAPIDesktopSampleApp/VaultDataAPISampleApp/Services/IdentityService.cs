using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;

using VaultDataAPISampleApp.Models;

namespace VaultDataAPISampleApp.Services
{
    internal sealed class IdentityService : IIdentityService
    {
        private static readonly TimeSpan s_authenticationTimeout = TimeSpan.FromMinutes(5);

        private readonly IdentityOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;

        public IdentityService(
            IOptions<IdentityOptions> options,
            IHttpClientFactory httpClientFactory)
        {
            _options = options.Value;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<AuthenticationResult> AuthenticateAsync(
            string clientId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentException("A client ID is required.", nameof(clientId));
            }

            return await AuthenticateCoreAsync(clientId, cancellationToken);
        }

        private async Task<AuthenticationResult> AuthenticateCoreAsync(
            string clientId,
            CancellationToken cancellationToken)
        {
            string codeVerifier = GenerateRandomString(64);
            string codeChallenge = CalculateCodeChallenge(codeVerifier);
            string state = GenerateRandomString(32);
            var callbackSource = new TaskCompletionSource<CallbackResult>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            await using WebApplication callbackApplication = CreateCallbackApplication(
                state,
                callbackSource);
            await callbackApplication.StartAsync(cancellationToken);
            try
            {
                string authorizationUrl = BuildAuthorizationUrl(clientId, codeChallenge, state);
                OpenDefaultBrowser(authorizationUrl);

                CallbackResult callback = await callbackSource.Task.WaitAsync(
                    s_authenticationTimeout,
                    cancellationToken);
                string authorizationCode = GetAuthorizationCode(callback);
                string accessToken = await ExchangeAuthorizationCodeAsync(
                    clientId,
                    codeVerifier,
                    authorizationCode,
                    cancellationToken);
                return new AuthenticationResult(accessToken);
            }
            finally
            {
                await callbackApplication.StopAsync(CancellationToken.None);
            }
        }

        private WebApplication CreateCallbackApplication(
            string state,
            TaskCompletionSource<CallbackResult> callbackSource)
        {
            WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(
                new WebApplicationOptions
                {
                    Args = []
                });
            builder.WebHost.ConfigureKestrel(options =>
                ConfigureCallbackListener(options, _options.RedirectUri));

            WebApplication callbackApplication = builder.Build();
            callbackApplication.MapGet(
                _options.RedirectUri.AbsolutePath,
                context => HandleCallbackAsync(context, state, callbackSource));
            return callbackApplication;
        }

        private static void ConfigureCallbackListener(
            KestrelServerOptions options,
            Uri redirectUri)
        {
            if (string.Equals(redirectUri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                options.ListenLocalhost(redirectUri.Port);
                return;
            }

            if (IPAddress.TryParse(redirectUri.Host, out IPAddress? address)
                && IPAddress.IsLoopback(address))
            {
                options.Listen(address, redirectUri.Port);
                return;
            }

            throw new InvalidOperationException("The OAuth callback must use a loopback address.");
        }

        private static async Task HandleCallbackAsync(
            HttpContext context,
            string expectedState,
            TaskCompletionSource<CallbackResult> callbackSource)
        {
            string returnedState = context.Request.Query["state"].ToString();
            if (!IsStateValid(expectedState, returnedState))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("The authorization state is invalid.");
                return;
            }

            string error = context.Request.Query["error"].ToString();
            string authorizationCode = context.Request.Query["code"].ToString();
            bool hasError = !string.IsNullOrWhiteSpace(error)
                || !IsAuthorizationCodeValid(authorizationCode);
            var callback = new CallbackResult(
                hasError ? null : authorizationCode,
                hasError);

            try
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync(GetBrowserResponseHtml(hasError));
            }
            finally
            {
                callbackSource.TrySetResult(callback);
            }
        }

        private string BuildAuthorizationUrl(
            string clientId,
            string codeChallenge,
            string state)
        {
            var parameters = new Dictionary<string, string>
            {
                ["response_type"] = "code",
                ["client_id"] = clientId,
                ["redirect_uri"] = _options.RedirectUri.AbsoluteUri,
                ["scope"] = _options.Scope,
                ["prompt"] = "login",
                ["state"] = state,
                ["code_challenge"] = codeChallenge,
                ["code_challenge_method"] = "S256"
            };
            var encodedParameters = parameters.Select(parameter =>
                $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}");
            string query = string.Join("&", encodedParameters);
            string authorizationUrl = $"{_options.AuthorizationEndpoint.AbsoluteUri}?{query}";
            return authorizationUrl;
        }

        private async Task<string> ExchangeAuthorizationCodeAsync(
            string clientId,
            string codeVerifier,
            string authorizationCode,
            CancellationToken cancellationToken)
        {
            using HttpClient client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("accept", "application/json");

            using var content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("code_verifier", codeVerifier),
                new KeyValuePair<string, string>("code", authorizationCode),
                new KeyValuePair<string, string>("redirect_uri", _options.RedirectUri.AbsoluteUri)
            ]);
            using HttpResponseMessage response = await client.PostAsync(
                _options.TokenEndpoint,
                content,
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"The token endpoint returned HTTP {(int)response.StatusCode}.");
            }

            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            AccessTokenResponse? tokenResponse = JsonSerializer.Deserialize<AccessTokenResponse>(
                responseContent,
                JsonSerializerOptions.Web);
            if (string.IsNullOrWhiteSpace(tokenResponse?.AccessToken))
            {
                throw new InvalidOperationException(
                    "The token response did not contain an access token.");
            }

            return tokenResponse.AccessToken;
        }

        private static string GetAuthorizationCode(CallbackResult callback)
        {
            if (callback.HasError || string.IsNullOrWhiteSpace(callback.AuthorizationCode))
            {
                throw new InvalidOperationException("Authorization was declined or failed.");
            }

            string authorizationCode = callback.AuthorizationCode;
            return authorizationCode;
        }

        private static bool IsAuthorizationCodeValid(string authorizationCode)
        {
            bool isValid = !string.IsNullOrWhiteSpace(authorizationCode)
                && authorizationCode.Length <= 4096
                && !authorizationCode.Any(char.IsControl);
            return isValid;
        }

        private static bool IsStateValid(string expectedState, string returnedState)
        {
            byte[] expectedBytes = Encoding.UTF8.GetBytes(expectedState);
            byte[] returnedBytes = Encoding.UTF8.GetBytes(returnedState);
            bool isValid = expectedBytes.Length == returnedBytes.Length
                && CryptographicOperations.FixedTimeEquals(expectedBytes, returnedBytes);
            return isValid;
        }

        private static void OpenDefaultBrowser(string authorizationUrl)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = authorizationUrl,
                UseShellExecute = true
            };

            using Process? browserProcess = Process.Start(startInfo);
        }

        private static string CalculateCodeChallenge(string codeVerifier)
        {
            byte[] challengeBytes = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
            string challenge = ConvertToBase64Url(challengeBytes);
            return challenge;
        }

        private static string GenerateRandomString(int byteCount)
        {
            byte[] randomBytes = RandomNumberGenerator.GetBytes(byteCount);
            string value = ConvertToBase64Url(randomBytes);
            return value;
        }

        private static string ConvertToBase64Url(byte[] value)
        {
            string result = Convert.ToBase64String(value)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
            return result;
        }

        private static string GetBrowserResponseHtml(bool hasError)
        {
            string title = hasError ? "Sign-in failed" : "Authorization received";
            string message = hasError
                ? "Return to the Vault Data API sample and try again."
                : "You can close this browser tab and return to the Vault Data API sample.";
            string html = $"""
                <!doctype html>
                <html lang="en">
                <head><meta charset="utf-8"><title>{title}</title></head>
                <body style="font-family:Segoe UI,Arial,sans-serif;padding:40px">
                  <h1>{title}</h1>
                  <p>{message}</p>
                </body>
                </html>
                """;
            return html;
        }

        private sealed record CallbackResult(
            string? AuthorizationCode,
            bool HasError);
    }
}
