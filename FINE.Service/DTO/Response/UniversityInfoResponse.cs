using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class UniversityInfoResponse
    {
        public int Id { get; set; }
        public int UniversityId { get; set; }
        public string? Domain { get; set; }
        public string? Role { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }
    }
}
