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
        public string? ProductName { get; set; } = null!;
        public List<BoxesAndQuantityMissing> ListBoxAndQuantity { get; set; }
    }

    public class BoxesAndQuantityMissing
    {
        public Guid BoxId { get; set; }
        public int Quantity { get; set; }
    }
}
