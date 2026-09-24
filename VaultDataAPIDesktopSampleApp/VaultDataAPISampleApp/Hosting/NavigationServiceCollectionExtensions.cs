using Microsoft.Extensions.DependencyInjection;

using VaultDataAPISampleApp.Navigation;

namespace VaultDataAPISampleApp.Hosting
{
    internal static class NavigationServiceCollectionExtensions
    {
        public static IServiceCollection AddNavigation(
            this IServiceCollection services)
        {
            services.AddSingleton<IPageRegistry, PageRegistry>();
            services.AddSingleton<IPageNavigationService, PageNavigationService>();
            return services;
        }
    }
}
