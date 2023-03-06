using FINE.Service.Commons;

namespace FINE.Service.DTO.Response
{
    public class ProductResponse
    {
        [Int]
        public int Id { get; set; }
        [Int]
        public int? GeneralProductId { get; set; }
        [String]
        public string? ProductCode { get; set; }
        [String]
        public string? ProductName { get; set; }
        public int CategoryId { get; set; }
        public int StoreId { get; set; }
        public double BasePrice { get; set; }
        public double? SizePrice { get; set; }
        public string? Size { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }
        public string? StoreName { get; set; }
    }
}
