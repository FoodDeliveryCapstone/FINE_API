using FINE.Service.Attributes;
using FINE.Service.DTO.Request.ProductInMenu;
using static FINE.Service.Helpers.Enum;

namespace FINE.Service.DTO.Request.Product
{
    public class UpdateProductRequest
    {
        public Guid StoreId { get; set; }

        public Guid CategoryId { get; set; }

        public string ProductCode { get; set; } = null!;

        public string ProductName { get; set; } = null!;

        public int ProductType { get; set; }

        public bool? IsStackable { get; set; }

        public string ImageUrl { get; set; } = null!;
        public bool IsActive { get; set; }
        //public List<UpdateProductAttributeRequest>? ProductAttribute { get; set; }
        //public List<UpdateProductInMenuRequest>? updateProductToMenu { get; set; }
    }

    public class UpdateProductActiveRequest
    {
        public bool IsActive { get; set; }
    }
}
