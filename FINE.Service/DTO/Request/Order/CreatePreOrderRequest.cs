using FINE.Data.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Request.Order
{
    public class CreatePreOrderRequest
    {
        public string? DeliveryPhone { get; set; }

        public int OrderType { get; set; }

        public int TimeSlotId { get; set; }

        public int RoomId { get; set; }

        public string? Note { get; set; }
        public ICollection<CreatePreOrderDetailRequest> OrderDetails { get; set; }
    }
    public class CreatePreOrderDetailRequest
    {
        public int ProductInMenuId { get; set; }

        public int? ComboId { get; set; }

        public int Quantity { get; set; }
                                              
        public string? Note { get; set; }
    }
    public class ListDetailByStore
    {
        public int StoreId { get; set; }

        public string StoreName { get; set; }

        public double TotalAmount { get; set; }

        public int TotalProduct { get; set; }

        public List<PreOrderDetailRequest> Details { get; set; }
    }
    public class PreOrderDetailRequest
    {
        public int ProductInMenuId { get; set; }

        public string? ProductCode { get; set; }

        public string ProductName { get; set; } = null!;
    
        public double? UnitPrice { get; set; }

        public int Quantity { get; set; }

        public double TotalAmount { get; set; }

        public double FinalAmount { get; set; }

        public int? ComboId { get; set; }
    }
}
