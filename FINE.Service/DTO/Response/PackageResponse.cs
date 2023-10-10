using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class PackageResponse
    {
        public int TotalProductInDay { get; set; }
        public List<ProductTotalDetail> productTotalDetails { get; set; }

    }
    public class ProductTotalDetail
    {
        public Guid ProductId { get; set; }
        public Guid ProductInMenuId { get; set; }
        public string ProductName { get; set; }
        public int PendingQuantity { get; set; }
        public int ReadyQuantity { get; set; }
        public int ErrorQuantity { get; set; }
        public List<ProductDetail> productDetails { get; set; }

    }
    public class ProductDetail
    {
        public Guid OrderId { get; set; }
        public int Quantity { get; set; }
        public bool IsReady { get; set; }
    }
}

