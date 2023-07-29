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
        public int OrderType { get; set; }

        public Guid TimeSlotId { get; set; }

        public List<CreatePreOrderDetailRequest> OrderDetails { get; set; }
    }
    public class CreatePreOrderDetailRequest
    {
        public Guid ProductId { get; set; }

        public int Quantity { get; set; }
                                              
        public string? Note { get; set; }
    }
}
