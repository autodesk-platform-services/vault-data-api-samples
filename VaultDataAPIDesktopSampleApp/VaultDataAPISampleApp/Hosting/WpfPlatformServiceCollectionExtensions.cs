using Microsoft.Extensions.DependencyInjection;

using VaultDataAPISampleApp.Dialogs;
using VaultDataAPISampleApp.Dialogs.Impl;
using VaultDataAPISampleApp.Platform;

namespace VaultDataAPISampleApp.Hosting
{
    internal static class WpfPlatformServiceCollectionExtensions
    {
        public static IServiceCollection AddWpfPlatform(
            this IServiceCollection services)
        {
            services.AddSingleton<IMessageDialogService, WpfMessageDialogService>();
            services.AddSingleton<IExternalUriLauncher, ExternalUriLauncher>();
            return services;
        }
    }
}
