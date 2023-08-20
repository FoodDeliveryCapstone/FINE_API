using FINE.Service.DTO.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Request.Order
{
    public class CreateOrderRequest
    {
        public Guid Id { get; set; }

        public string OrderCode { get; set; }

        public string? PartyCode { get; set; }

        public double TotalAmount { get; set; }

        public double FinalAmount { get; set; }

        public double TotalOtherAmount { get; set; }

        public int OrderType { get; set; }

        public Guid TimeSlotId { get; set; }

        public string StationId { get; set; }

        public int PaymentType { get; set; }

        public bool IsPartyMode { get; set; }

        public int ItemQuantity { get; set; }

        public int Point { get; set; }

        public List<CreateOrderDetail> OrderDetails { get; set; }

        public List<OrderOtherAmount> OtherAmounts { get; set; }
    }
    public class CreateOrderDetail
    {
        public Guid Id { get; set; }

        public Guid OrderId { get; set; }

        public Guid ProductInMenuId { get; set; }

        public Guid StoreId { get; set; }

        public string ProductCode { get; set; } = null!;

        public string ProductName { get; set; } = null!;

        public double UnitPrice { get; set; }

        public int Quantity { get; set; }

        public double TotalAmount { get; set; }

        public double FinalAmount { get; set; }

        public string? Note { get; set; }
    }
}
