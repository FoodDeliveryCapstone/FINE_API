using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Request.University
{
    public class UpdateUniversityRequest
    {
        public string UniName { get; set; } = null!;
        public string UniCode { get; set; } = null!;
        public string? ContactName { get; set; }
        public string? ContactEmail { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
    }
}
