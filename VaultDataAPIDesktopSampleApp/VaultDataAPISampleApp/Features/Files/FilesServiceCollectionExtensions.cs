using Microsoft.Extensions.DependencyInjection;

using VaultDataAPISampleApp.Features.Files.ViewModels;
using VaultDataAPISampleApp.Features.Files.Views;
using VaultDataAPISampleApp.Navigation;

namespace VaultDataAPISampleApp.Features.Files
{
    internal static class FilesServiceCollectionExtensions
    {
        public static IServiceCollection AddFilesSample(
            this IServiceCollection services)
        {
            services.AddSingleton<FilesViewModel>();
            services.AddSamplePage<FilesPage>(SamplePageId.Files);
            return services;
        }
    }
}
