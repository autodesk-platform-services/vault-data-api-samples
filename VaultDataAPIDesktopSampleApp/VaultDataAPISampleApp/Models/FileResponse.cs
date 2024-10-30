using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VaultDataAPISampleApp.Models
{
    public class FileResponse
    {
        public string Id { get; set; }
        public FileVersionResponse FileVersion { get; set; }
        public string VersionType { get; set; }
        public string EntityType { get; set; }
        public string Url { get; set; }
    }
}
