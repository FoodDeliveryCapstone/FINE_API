using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class StaffResponse
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public byte[]? Password { get; set; }
        public string? RoleType { get; set; }
        public int? AreaId { get; set; }
        public bool? IsAvailable { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }
    }
}
