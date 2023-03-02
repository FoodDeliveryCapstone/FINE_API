using FINE.Data.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class GenOrderResponse
    {
        public int Id { get; set; }

        public int? GeneralOrderId { get; set; }

        public string OrderCode { get; set; } = null!;

        public OrderCustomerResponse Customer { get; set; }

        public string DeliveryPhone { get; set; } = null!;

        public DateTime CheckInDate { get; set; }

        public double TotalAmount { get; set; }

        public double? Discount { get; set; }

        public double FinalAmount { get; set; }

        public double ShippingFee { get; set; }

        public int OrderStatus { get; set; }

        public int OrderType { get; set; }

        public int TimeSlotId { get; set; }

        public int RoomId { get; set; }

        //public int StoreId { get; set; }

        public bool IsConfirm { get; set; }

        public bool IsPartyMode { get; set; }

        public int? ShipperId { get; set; }

        public string? Note { get; set; }
        public List<OrderResponse> InverseGeneralOrder { get; set; }
    }

    public class OrderResponse
    {
        public int Id { get; set; }

        public int? GeneralOrderId { get; set; }

        public string OrderCode { get; set; } = null!;

        public int StoreId { get; set; }

        public int OrderStatus { get; set; }

        public bool IsConfirm { get; set; }

        public List<OrderDetailResponse> OrderDetails { get; set; }

    }
    public class OrderDetailResponse
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public int ProductInMenuId { get; set; }

        public string ProductCode { get; set; } = null!;

        public string ProductName { get; set; } = null!;

        public int? ComboId { get; set; }

        public double UnitPrice { get; set; }

        public int Quantity { get; set; }

        public double TotalAmount { get; set; }

        public double? Discount { get; set; }

        public double FinalAmount { get; set; }

        public string? Note { get; set; }
    }

    public class OrderCustomerResponse
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string CustomerCode { get; set; } = null!;

        public string? Email { get; set; }

        public string? Phone { get; set; }
    }
}
