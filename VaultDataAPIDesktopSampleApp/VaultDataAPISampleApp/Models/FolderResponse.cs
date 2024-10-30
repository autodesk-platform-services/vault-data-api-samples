using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VaultDataAPISampleApp.Models
{
    public class FolderResponse
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string FullName { get; set; }
        public int CategoryColor { get; set; }
        public int StateColor { get; set; }
        public int SubfolderCount { get; set; }
        public bool IsLibrary { get; set; }
        public bool IsCloaked { get; set; }
        public bool IsReadOnly { get; set; }
        public DateTime CreateDate { get; set; }
        public string CreateUserName { get; set; }
        public string EntityType { get; set; }
        public string Url { get; set; } 
    }
}
