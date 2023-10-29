using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;

namespace FINE.Service.DTO.Request.Package
{
    public class UpdateProductPackageRequest
    {
        public string TimeSlotId { get; set; }
        public string? StoreId { get; set; }
        public PackageUpdateTypeEnum Type { get; set; }
        public List<string> ProductsUpdate { get; set; }
        public int? Quantity { get; set; } = 0;
        public Guid? BoxId { get; set; }
    }
}
