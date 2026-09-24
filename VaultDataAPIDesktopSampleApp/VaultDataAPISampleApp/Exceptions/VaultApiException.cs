using System.Net;

namespace VaultDataAPISampleApp.Exceptions;

public sealed class VaultApiException : Exception
{
    public VaultApiException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}
