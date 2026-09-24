using System.Collections.Generic;

namespace VaultDataAPISampleApp.Models
{
    public sealed class FolderContentsResponse
    {
        public List<FolderResponse> Folders { get; } = [];

        public List<FileVersionResponse> Files { get; } = [];
    }
}
