using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace VaultDataAPISampleApp.Http
{
    internal sealed class VaultApiTimeoutHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            using var timeoutSource =
                new CancellationTokenSource(VaultApiHttpClient.RequestTimeout);
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token);

            try
            {
                return await base.SendAsync(request, linkedSource.Token);
            }
            catch (OperationCanceledException exception)
                when (!cancellationToken.IsCancellationRequested
                    && timeoutSource.IsCancellationRequested)
            {
                throw new TimeoutException(
                    "Vault API did not respond within one minute. "
                    + "The request was canceled.",
                    exception);
            }
        }
    }
}
