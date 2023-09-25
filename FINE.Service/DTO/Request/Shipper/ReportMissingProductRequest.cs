using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Request.Shipper
{
    public class ReportMissingProductRequest
    {
        public Guid TimeSlotId { get; set; }
        public Guid StationId { get; set; }
        public Guid StoreId { get; set; }
        public Guid BoxId { get; set; }
        public List<MissingProductRequest> MissingProducts { get; set; }
    }

    public class MissingProductRequest
    {
        public string? ProductName { get; set; } = null!;
        public int Quantity { get; set; }
    }
}
