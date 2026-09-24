using Microsoft.Extensions.DependencyInjection;

using VaultDataAPISampleApp.Features.Authentication.Dialogs;
using VaultDataAPISampleApp.Features.Authentication.ViewModels;

namespace VaultDataAPISampleApp.Features.Authentication
{
    internal static class AuthenticationServiceCollectionExtensions
    {
        public static IServiceCollection AddAuthenticationFeature(
            this IServiceCollection services)
        {
            services.AddSingleton<LoginViewModelFactory>();
            services.AddSingleton<ISignInDialogService, WpfSignInDialogService>();
            return services;
        }
    }
}
