using VaultDataAPISampleApp.Services;

namespace VaultDataAPISampleApp.Features.Authentication.ViewModels
{
    internal sealed class LoginViewModelFactory
    {
        private readonly IIdentityService _identityService;

        public LoginViewModelFactory(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        public LoginViewModel Create(string clientId)
        {
            return new LoginViewModel(_identityService, clientId);
        }
    }
}
