using System.Windows.Controls;

using Microsoft.Extensions.DependencyInjection;

namespace VaultDataAPISampleApp.Navigation
{
    internal static class PageRegistrationServiceCollectionExtensions
    {
        public static IServiceCollection AddSamplePage<TPage>(
            this IServiceCollection services,
            SamplePageId pageId)
            where TPage : Page
        {
            services.AddSingleton<TPage>();
            services.AddSingleton(serviceProvider => new PageRegistration(
                pageId,
                serviceProvider.GetRequiredService<TPage>));

            return services;
        }
    }
}
