using FINE.Service.DTO.Request.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Request.ProductInMenu
{
    public class AddProductToMenuRequest
    {
        public int? MenuId { get; set; }

        public List<ProductInMenuRequest>? addProducts { get; set; }
    }
    public class ProductInMenuRequest
    {
        public int? ProductId { get; set; }
        public double? Price { get; set; }
        public int Status { get; set; }
    }
}
