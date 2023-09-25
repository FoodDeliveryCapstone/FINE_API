using FINE.Service.DTO.Request.Shipper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class ShipperResponse
    {
    }

    public class ReportMissingProductResponse
    {
        public Guid ReportId { get; set; }
        public Guid TimeSlotId { get; set; }
        public Guid StationId { get; set; }
        public Guid StoreId { get; set; }
        public Guid BoxId { get; set; }
        public List<MissingProductRequest> MissingProducts { get; set; }
    }
}
