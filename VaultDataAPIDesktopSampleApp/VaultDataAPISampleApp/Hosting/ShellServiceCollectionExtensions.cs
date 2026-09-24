using Microsoft.Extensions.DependencyInjection;

using VaultDataAPISampleApp.ViewModels;

namespace VaultDataAPISampleApp.Hosting
{
    internal static class ShellServiceCollectionExtensions
    {
        public static IServiceCollection AddShell(this IServiceCollection services)
        {
            services.AddSingleton<ShellViewModel>();
            services.AddSingleton(serviceProvider => new MainWindow(
                serviceProvider.GetRequiredService<ShellViewModel>()));
            return services;
        }
    }
}
