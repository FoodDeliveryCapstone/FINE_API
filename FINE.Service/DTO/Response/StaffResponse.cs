using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;

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

    public class OrderDetailForStaffResponse
    {
        public Guid Id { get; set; }

        public Guid OrderId { get; set; }

        public Guid ProductInMenuId { get; set; }

        public Guid ProductId { get; set; }

        public Guid StoreId { get; set; }

        public string? ProductCode { get; set; } = null!;

        public string? ProductName { get; set; } = null!;

        public double UnitPrice { get; set; }

        public int Quantity { get; set; }

        public double TotalAmount { get; set; }

        public double? Discount { get; set; }

        public double FinalAmount { get; set; }

        public string? Note { get; set; }
        [NotMapped]
        public OrderStatusEnum? OrderDetailProductStatus = OrderStatusEnum.Processing;
    }

    public class OrderForStaffResponse
    {
        public Guid Id { get; set; }
        public string? OrderCode { get; set; } = null!;

        public Guid CustomerId { get; set; }
        public DateTime? CheckInDate { get; set; }
        public double? FinalAmount { get; set; }

        public int? OrderStatus { get; set; }

        public int? OrderType { get; set; }

        public TimeSlotOrderResponse? TimeSlot { get; set; }

        public Guid StationId { get; set; }

        public bool? IsConfirm { get; set; }

        public bool? IsPartyMode { get; set; }

        public int? ItemQuantity { get; set; }

        public string? Note { get; set; }

        public List<OrderDetailResponse>? OrderDetails { get; set; }

    }

    public class OrderByStoreResponse
    {
        public Guid OrderId { get; set; }
        public Guid StoreId { get; set; }
        public string? CustomerName { get; set; }
        public TimeSlotOrderResponse TimeSlot { get; set; }
        public Guid StationId { get; set; }
        public string? StationName { get; set; }
        public DateTime? CheckInDate { get; set; }
        public int? OrderType { get; set; }
        [NotMapped]
        public OrderStatusEnum OrderDetailStoreStatus { get; set; }
        public List<OrderDetailForStaffResponse>? OrderDetails { get; set; }
    }

    public class SplitOrderResponse
    {
        public List<Guid> OrderDetailId { get; set; }
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
        public List<OrderDetailForStaffResponse> OrderDetails { get; set; }
    }
}
