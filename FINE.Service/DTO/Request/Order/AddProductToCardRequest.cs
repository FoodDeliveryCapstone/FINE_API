using FINE.Service.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;

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

    public class AddProductToCardRequestV2
    {
        public OrderTypeEnum OrderType { get; set; }
        public string? TimeSlotId { get; set; }

        public string ProductId { get; set; }

        public CubeModel RemainingLengthSpace { get; set; }

        public CubeModel RemainingWidthSpace { get; set; }
    }

}
