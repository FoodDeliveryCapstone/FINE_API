using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Request.UniversityInfo
{
    public class CreateUniversityInfoRequest
    {
        public int UniversityId { get; set; }
        public string Domain { get; set; } = null!;
        public string Role { get; set; } = null!;
    }
}
