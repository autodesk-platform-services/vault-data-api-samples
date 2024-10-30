using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VaultDataAPISampleApp.Models
{
    public class SessionResponse
    {
        public string Id { get; set; }
        public string Authorization { get; set; }
        public DateTime CreateDate { get; set; }
        public VaultInformation VaultInformation { get; set; }
        public UserInformation UserInformation { get; set; }
        public string Url { get; set; }
    }

    public class VaultInformation
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
    }

    public class UserInformation
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string SystemName { get; set; }
        public string Email { get; set; }
        public string AuthTypes { get; set; }
        public DateTime CreateDate { get; set; }
        public bool IsActive { get; set; }
        public string Url { get; set; }
    }
}
