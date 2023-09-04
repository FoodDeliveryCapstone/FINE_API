using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class AddProductToCardResponse
    {
        public StatusViewModel Status { get; set; }
        public ProductInCardResponse Product { get; set; }

        public List<ProductInCardResponse> Card { get; set; }

        public List<ProductRecommend> ProductsRecommend { get; set; }

    }

    public class ProductInCardResponse
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;

        public string Code { get; set; } = null!;

        public string? Size { get; set; }

        public int Quantity { get; set; }

        public double Price { get; set; }
    }

    public class ProductRecommend
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;

        public string Code { get; set; } = null!;

        public string? Size { get; set; }

        public double Price { get; set; }
    }
}
