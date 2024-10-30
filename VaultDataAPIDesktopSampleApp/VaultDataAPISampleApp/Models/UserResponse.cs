using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VaultDataAPISampleApp.Models
{
    public class UserResponse
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
