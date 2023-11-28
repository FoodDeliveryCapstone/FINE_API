using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ServiceStack.LicenseUtils;

namespace FINE.Service.DTO.Request.Staff
{
    public class UpdateStaffRequest
    {
        public string Name { get; set; } = null!;
        public int? RoleType { get; set; }
        public Guid? StoreId { get; set; }
        public Guid? StationId { get; set; }
        public bool? IsActive { get; set; }
    }
}
