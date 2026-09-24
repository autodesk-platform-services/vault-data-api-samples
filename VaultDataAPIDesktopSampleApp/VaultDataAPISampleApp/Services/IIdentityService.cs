using System.Threading;
using System.Threading.Tasks;

namespace VaultDataAPISampleApp.Services
{
    /// <summary>
    /// Authenticates a user through the system browser.
    /// </summary>
    public interface IIdentityService
    {
        /// <summary>
        /// Runs the OAuth authorization-code flow.
        /// </summary>
        /// <param name="clientId">The APS application client ID.</param>
        /// <param name="cancellationToken">Cancels the interactive authentication operation.</param>
        /// <returns>The authentication result.</returns>
        Task<AuthenticationResult> AuthenticateAsync(
            string clientId,
            CancellationToken cancellationToken);
    }
}
