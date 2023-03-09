using FINE.Service.Commons;
using FINE.Service.DTO.Request.ProductInMenu;

namespace FINE.Service.DTO.Request.Product
{
    public class CreateProductRequest
    {
        [String]
        public string ProductCode { get; set; }
        [String]
        public string ProductName { get; set; }
        [Int]
        public int CategoryId { get; set; }
        [Int]
        public int StoreId { get; set; }
        public double BasePrice { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public int? ProductType { get; set; }
        public string? Description { get; set; }

        public List<CreateExtraProductRequest> extraProducts { get; set; }
        public List<AddProductToMenuRequest> addProductToMenu { get; set; }
    }

    public class CreateExtraProductRequest
    {
        public double? SizePrice { get; set; }
        public string? Size { get; set; }
    }
}
