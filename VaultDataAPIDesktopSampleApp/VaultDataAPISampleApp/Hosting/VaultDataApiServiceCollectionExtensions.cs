using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using VaultDataAPISampleApp.Configuration;
using VaultDataAPISampleApp.Http;
using VaultDataAPISampleApp.Services;
using VaultDataAPISampleApp.Services.Impl;
using VaultDataAPISampleApp.State;

namespace VaultDataAPISampleApp.Hosting
{
    internal static class VaultDataApiServiceCollectionExtensions
    {
        public static IServiceCollection AddVaultDataApi(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services
                .AddOptions<VaultOptions>()
                .Bind(configuration.GetSection(VaultOptions.SectionName))
                .Validate(
                    options => VaultOptions.IsValidApiBaseUri(options.ApiBaseUri),
                    "ApiBaseUri must be a root-relative path without query or fragment.")
                .ValidateOnStart();

            services.AddTransient<VaultApiTimeoutHandler>();
            services
                .AddHttpClient(
                    VaultApiHttpClient.Name,
                    client => client.Timeout = System.Threading.Timeout.InfiniteTimeSpan)
                .AddHttpMessageHandler<VaultApiTimeoutHandler>();

            services.AddSingleton<IVaultDataApiService, VaultDataApiService>();
            services.AddSingleton<VaultSession>();

            return services;
        }
    }
}
