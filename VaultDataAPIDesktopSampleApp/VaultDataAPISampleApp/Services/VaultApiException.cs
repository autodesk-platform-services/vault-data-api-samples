using System.Net;

namespace VaultDataAPISampleApp.Services;

public sealed class VaultApiException : Exception
{
    public VaultApiException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}
