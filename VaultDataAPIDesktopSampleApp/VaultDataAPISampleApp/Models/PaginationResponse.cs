using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VaultDataAPISampleApp.Models
{
    public class PaginationResponse<T>
    {
        public Pagination Pagination { get; set; }
        public List<T> Results { get; set; }
    }

    public class Pagination
    {
        public int Limit { get; set; }
        public int TotalResults { get; set; }
        public string NextUrl { get; set; }
        public string IndexingStatus { get; set; }
    }
}
