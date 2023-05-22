using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class RevenueResponse
    {
        public double? TotalRevenueBeforeDiscount { get; set; }
        public double? TotalDiscount { get; set; }
        public double? TotalShippingFee { get; set; }
        public double TotalRevenueAfterDiscount { get; set; }    

    }

    public class StoreRevenueResponse
    {
        public int? StoreId { get; set; }
        public string? StoreName { get; set; }
        public double? TotalRevenueBeforeDiscount { get; set; }
        public double? TotalDiscount { get; set; }
        public double TotalRevenueAfterDiscount { get; set; }

    }
}
