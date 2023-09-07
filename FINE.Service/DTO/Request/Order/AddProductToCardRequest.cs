using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Request.Order
{
    public class AddProductToCardRequest
    {
        public string? TimeSlotId { get; set; }

        public string ProductId { get; set; }

        public int Quantity { get; set; }

        public List<ProductInCardRequest>? Card { get; set; }
    }
    public class ProductInCardRequest
    {
        public string ProductId { get; set; }

        public int Quantity { get; set; }
    }

}
