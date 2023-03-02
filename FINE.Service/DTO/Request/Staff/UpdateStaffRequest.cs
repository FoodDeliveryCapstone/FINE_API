using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Request.Staff
{
    public class UpdateStaffRequest
    {
        public string Username { get; set; } = null!;
        public byte[] Password { get; set; } = null!;
        public string? RoleType { get; set; }
        public int? AreaId { get; set; }
        public bool? IsAvailable { get; set; }
    }
}
