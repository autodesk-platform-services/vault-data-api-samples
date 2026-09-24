using Microsoft.Extensions.DependencyInjection;

using VaultDataAPISampleApp.Features.ExternalSync.Dialogs;
using VaultDataAPISampleApp.Features.ExternalSync.ViewModels;
using VaultDataAPISampleApp.Features.ExternalSync.Views;
using VaultDataAPISampleApp.Navigation;

namespace VaultDataAPISampleApp.Features.ExternalSync
{
    internal static class ExternalSyncServiceCollectionExtensions
    {
        public static IServiceCollection AddExternalSyncSample(
            this IServiceCollection services)
        {
            services.AddSingleton<IExternalSyncDialogService, WpfExternalSyncDialogService>();
            services.AddSingleton<ExternalSyncViewModel>();
            services.AddSamplePage<ExternalSyncPage>(SamplePageId.ExternalSync);
            return services;
        }
    }
}
