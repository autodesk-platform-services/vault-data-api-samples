using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VaultDataAPISampleApp.Models
{
    public class FileVersionResponse
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string State { get; set; }
        public int StateColor { get; set; }
        public string Revision { get; set; }
        public string Category { get; set; }
        public int CategoryColor { get; set; }
        public string Classification { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public bool IsCheckedOut { get; set; }
        public bool HasVisualizationAttachment { get; set; }
        public string VisualizationAttachmentStatus { get; set; }
        public int Version { get; set; }
        public int Size { get; set; }
        public bool IsCloaked { get; set; }
        public DateTime CheckinDate { get; set; }
        public bool IsHidden { get; set; }
        public bool IsReadOnly { get; set; }
        public FileResponse File { get; set; }
        public FolderResponse Parent { get; set; }
        public DateTime CreateDate { get; set; }
        public string CreateUserName { get; set; }
        public string EntityType { get; set; }
        public string Url { get; set; }
    }
}
