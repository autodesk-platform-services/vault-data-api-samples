using System.Text.Json.Serialization;

namespace VaultDataAPISampleApp.Models
{
    public sealed class FileUploadSessionResponse
    {
        public required string UploadId { get; set; }

        public required string UploadSessionToken { get; set; }

        public string? State { get; set; }

        public long MaxPartSize { get; set; }

        public long TotalBytes { get; set; }
    }

    public sealed class FileUploadCompletionResponse
    {
        public required string UploadId { get; set; }

        public string? State { get; set; }

        public long Size { get; set; }

        public int Checksum { get; set; }

        public required string UploadTicket { get; set; }
    }

    public sealed class AddFileRequest
    {
        public long FolderId { get; set; }

        public required string UploadTicket { get; set; }

        public required string Name { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Comment { get; set; }
    }

    public sealed class CheckinFileRequest
    {
        public required string UploadTicket { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Comment { get; set; }

        public bool KeepCheckedOut { get; set; }
    }
}
