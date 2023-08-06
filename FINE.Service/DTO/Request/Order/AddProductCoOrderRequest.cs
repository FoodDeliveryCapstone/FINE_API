using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Request.Order
{
    public class AddProductCoOrderRequest
    {
        public Guid? TimeSlotId { get; set; }

        public Guid ProductId { get; set; }

        public int Quantity { get; set; }

        public string? Note { get; set; }

    }
}
