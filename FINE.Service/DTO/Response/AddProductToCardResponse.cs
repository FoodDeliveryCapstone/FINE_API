using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class AddProductToCardResponse
    {
        public ProductInCard Product { get; set; }
        public double VolumeRemainingSpace { get; set; }
        public List<ProductRecommend> ProductsRecommend { get; set; }

    }

    public class ProductInCard
    {
        public StatusViewModel Status { get; set; }

        public Guid Id { get; set; }

        public string Name { get; set; } = null!;

        public string Code { get; set; } = null!;

        public string? Size { get; set; }

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
