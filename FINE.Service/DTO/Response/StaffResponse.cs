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

        public string? Name { get; set; } = null!;

        public string? Username { get; set; } = null!;

        public int RoleType { get; set; }

        public int? StoreId { get; set; }

        public int? CustomerId { get; set; }

        public bool? IsAvailable { get; set; }

        public DateTime CreateAt { get; set; }

        public DateTime? UpdateAt { get; set; }
    }
}
