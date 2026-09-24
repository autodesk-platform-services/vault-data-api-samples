using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using VaultDataAPISampleApp.Configuration;
using VaultDataAPISampleApp.Services;
using VaultDataAPISampleApp.Services.Impl;

namespace VaultDataAPISampleApp.Hosting
{
    internal static class OxygenIdentityServiceCollectionExtensions
    {
        public static IServiceCollection AddOxygenIdentity(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services
                .AddOptions<IdentityOptions>()
                .Bind(configuration.GetSection(IdentityOptions.SectionName))
                .Validate(
                    options => IdentityOptions.IsAbsoluteHttps(options.AuthorizationEndpoint),
                    "AuthorizationEndpoint must be an absolute HTTPS URL.")
                .Validate(
                    options => IdentityOptions.IsAbsoluteHttps(options.TokenEndpoint),
                    "TokenEndpoint must be an absolute HTTPS URL.")
                .Validate(
                    options => IdentityOptions.IsLoopbackHttp(options.RedirectUri),
                    "RedirectUri must be a loopback HTTP URL without query or fragment.")
                .Validate(
                    options => !string.IsNullOrWhiteSpace(options.Scope),
                    "Scope is required.")
                .ValidateOnStart();

            services.AddHttpClient();
            services.AddSingleton<IIdentityService, IdentityService>();

            return services;
        }
    }
}
