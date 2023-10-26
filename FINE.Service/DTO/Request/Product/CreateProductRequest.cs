using FINE.Service.Attributes;
using FINE.Service.DTO.Request.ProductInMenu;
using static FINE.Service.Helpers.Enum;

namespace FINE.Service.DTO.Request.Product
{
    public class CreateProductRequest
    {
        public Guid StoreId { get; set; }

        public Guid CategoryId { get; set; }

        public string ProductCode { get; set; } = null!;

        public string ProductName { get; set; } = null!;

        public int ProductType { get; set; }

        public bool? IsStackable { get; set; }

        public string ImageUrl { get; set; } = null!;

        public List<CreateProductAttributeRequest>? ProductAttribute { get; set; }
    }

    public class CreateProductAttributeRequest
    {
        public string? Size { get; set; }

        public double Price { get; set; }
        public ProductRotationTypeEnum RotationType { get; set; }

        public double Height { get; set; }

        public double Width { get; set; }

        public double Length { get; set; }
    }

    public class CreateProductPassio
    {
        public string ProductName { get; set; }
        public double Price { get; set; }
        public string PicURL { get; set; }
        public string Code { get; set; }
    }
}
