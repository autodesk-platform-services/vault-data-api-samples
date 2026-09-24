using System;

namespace VaultDataAPISampleApp.Services
{
    internal sealed class IdentityOptions
    {
        public const string SectionName = "Identity";

        public Uri AuthorizationEndpoint { get; set; } = default!;

        public Uri TokenEndpoint { get; set; } = default!;

        public Uri RedirectUri { get; set; } = default!;

        public string Scope { get; set; } = string.Empty;

        public static bool IsAbsoluteHttps(Uri? uri)
        {
            return uri is { IsAbsoluteUri: true }
                && string.Equals(
                    uri.Scheme,
                    Uri.UriSchemeHttps,
                    StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsLoopbackHttp(Uri? uri)
        {
            return uri is { IsAbsoluteUri: true, IsLoopback: true }
                && string.Equals(
                    uri.Scheme,
                    Uri.UriSchemeHttp,
                    StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrEmpty(uri.Query)
                && string.IsNullOrEmpty(uri.Fragment);
        }
    }
}
