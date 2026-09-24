using Microsoft.Extensions.DependencyInjection;

using VaultDataAPISampleApp.Features.Users.ViewModels;
using VaultDataAPISampleApp.Features.Users.Views;
using VaultDataAPISampleApp.Navigation;

namespace VaultDataAPISampleApp.Features.Users
{
    internal static class UsersServiceCollectionExtensions
    {
        public static IServiceCollection AddUsersSample(
            this IServiceCollection services)
        {
            services.AddSingleton<UsersViewModel>();
            services.AddSamplePage<UsersPage>(SamplePageId.Users);
            return services;
        }
    }
}
