using FINE.Service.DTO.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Request.Order
{
    public class CreateGenOrderRequest
    {
        public string OrderCode { get; set; }

        public int CustomerId { get; set; }

        public string DeliveryPhone { get; set; }

        public double TotalAmount { get; set; }

        public double? Discount { get; set; }

        public double FinalAmount { get; set; }

        public double ShippingFee { get; set; }

        public int OrderType { get; set; }

        public int TimeSlotId { get; set; }

        public int RoomId { get; set; }

        public List<CreateOrderRequest> InverseGeneralOrders { get; set; }
    }

    public class CreateOrderRequest
    {
        public string OrderCode { get; set; } = null!;

        public double TotalAmount { get; set; }

        public double? Discount { get; set; }

        public double FinalAmount { get; set; }

        public int? StoreId { get; set; }
        public string? Note { get; set; }

        public List<CreateOrderDetailRequest> OrderDetails { get; set; }
    }

        public class CreateOrderDetailRequest
    {
        public int ProductInMenuId { get; set; }

        public string ProductCode { get; set; }

        public string ProductName { get; set; }

        public int? ComboId { get; set; }

        public double UnitPrice { get; set; }

        public int Quantity { get; set; }

        public double TotalAmount { get; set; }

        public double FinalAmount { get; set; }

        public string? Note { get; set; }
    }
}
