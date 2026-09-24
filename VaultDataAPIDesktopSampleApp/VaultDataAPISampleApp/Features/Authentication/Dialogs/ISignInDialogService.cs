using VaultDataAPISampleApp.Models;

namespace VaultDataAPISampleApp.Features.Authentication.Dialogs
{
    public interface ISignInDialogService
    {
        AuthenticationResult? SignIn(string clientId);
    }
}
