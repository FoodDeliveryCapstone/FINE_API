using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class StaffResponse
    {
        public Guid Id { get; set; }

        public string? Name { get; set; } = null!;

        public string? Username { get; set; } = null!;

        public int RoleType { get; set; }

        public Guid? StoreId { get; set; }

        public bool? IsActive { get; set; }

        public DateTime? CreateAt { get; set; }

        public DateTime? UpdateAt { get; set; }
    }

    public class SplitOrderResponse
    {
        public string? ProductName { get; set; } = null!;
        public int Quantity { get; set; }
        public Guid TimeSlotId { get; set; }
    }

    public class ShipperSplitOrderResponse
    {
        public Guid TimeSlotId { get; set; }
        public Guid StationId { get; set; }
        public Guid StoreId { get; set; }
        public string? ProductName { get; set; } = null!;
        public int Quantity { get; set; }
    }

    public class ShipperOrderBoxResponse
    {
        public Guid BoxId { get; set; }
        public List<OrderDetailResponse> OrderDetails { get; set; }
    }
}
