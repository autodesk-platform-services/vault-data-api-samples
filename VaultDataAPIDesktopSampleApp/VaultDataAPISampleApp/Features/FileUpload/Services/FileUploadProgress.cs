namespace VaultDataAPISampleApp.Features.FileUpload.Services
{
    public sealed record FileUploadProgress(
        string Status,
        long BytesTransferred = 0,
        long TotalBytes = 0);
}
