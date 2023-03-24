using FINE.Data.Entity;
using FINE.Service.Commons;

namespace FINE.Service.DTO.Response
{
    public class ProductResponse
    {
        [Int] public int Id { get; set; }
        [Int] public int? GeneralProductId { get; set; }
        [String] public string? ProductCode { get; set; }
        [String] public string? ProductName { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int StoreId { get; set; }
        public string? StoreName { get; set; }
        public string? ImageUrl { get; set; }
        public double BasePrice { get; set; }
        public double? SizePrice { get; set; }
        public string? Size { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }

        
    }

    public class ProductInMenuResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        [Int] public int? GeneralProductId { get; set; }
        [String] public string? ProductCode { get; set; }
        [String] public string? ProductName { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int StoreId { get; set; }
        public string? StoreName { get; set; }
        public string? ImageUrl { get; set; }

        public double? Price { get; set; }

        public bool IsAvailable { get; set; }

        public int Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}