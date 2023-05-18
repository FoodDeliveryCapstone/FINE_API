using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class UniversityResponse
    {
        public int? Id { get; set; }
        public string? UniName { get; set; }
        public string? UniCode { get; set; }
        public string? ContactName { get; set; }
        public string? ContactEmail { get; set; }
        public string? ImageUrl { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }
    }
}
