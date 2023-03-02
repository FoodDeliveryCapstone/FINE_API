using FINE.Data.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Request.Order
{
    public class CreateOrderRequest
    {
        //public int Id { get; set; }

        //public string OrderCode { get; set; } = null!;

        public int CustomerId { get; set; }

        public string? DeliveryPhone { get; set; }

        //public DateTime CheckInDate { get; set; }

        public double TotalAmount { get; set; }

        //public double? Discount { get; set; }

        //public double FinalAmount { get; set; }

        //public double ShippingFee { get; set; }

        //public int OrderStatus { get; set; }

        public int OrderType { get; set; }

        public int TimeSlotId { get; set; }

        public int RoomId { get; set; }

        //public int StoreId { get; set; }

        public bool IsConfirm { get; set; }

        //public bool IsPartyMode { get; set; }

        //public int? ShipperId { get; set; }

        public string? Note { get; set; }
        public ICollection<CreateOrderDetailRequest> OrderDetails { get; set; }
    }
    public class CreateOrderDetailRequest
    {
        //public int Id { get; set; }

        //public int OrderId { get; set; }

        public int ProductInMenuId { get; set; }

        public string ProductCode { get; set; } 

        public string ProductName { get; set; }
        public int StoreId { get; set; }

        public int? ComboId { get; set; }

        public double UnitPrice { get; set; }

        public int Quantity { get; set; }

        public double TotalAmount { get; set; }

        //public double? Discount { get; set; }

        public double FinalAmount { get; set; }

        public string? Note { get; set; }
    }
    public class OrderDetailByStoreRequest
    {
        public int StoreId { get; set; }

        public List<CreateOrderDetailRequest> Details { get; set; }
    }
}
