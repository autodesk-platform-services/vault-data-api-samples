using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VaultDataAPISampleApp.Models
{
    public class GroupResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string SystemName { get; set; }
        public string AuthTypes { get; set; }
        public DateTime CreateDate { get; set; }
        public string EmailDL { get; set; }
        public bool IsActive { get; set; }
        public string Url { get; set; }
    }
}
