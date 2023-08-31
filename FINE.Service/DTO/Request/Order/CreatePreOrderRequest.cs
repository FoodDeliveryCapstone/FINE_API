using FINE.Data.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;

namespace FINE.Service.DTO.Request.Order
{
    public class CreatePreOrderRequest
    {
        public OrderTypeEnum? OrderType { get; set; }

        public PartyOrderType? PartyType { get; set; }

        public Guid? TimeSlotId { get; set; }

        public List<CreatePreOrderDetailRequest>? OrderDetails { get; set; }
    }
    public class CreatePreOrderDetailRequest
    {
        public Guid ProductId { get; set; }

        public int Quantity { get; set; }
                                              
        public string? Note { get; set; }
    }
}
