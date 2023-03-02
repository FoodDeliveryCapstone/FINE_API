using FINE.Service.Commons;

namespace FINE.Service.DTO.Request.Product
{
    public class UpdateProductRequest
    {
        public string ProductCode { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public int CategoryId { get; set; }
        public double BasePrice { get; set; }
        public bool IsActive { get; set; }
        public List<UpdateProductExtraRequest> extraProduct { get; set; }
    }
    public class UpdateProductExtraRequest
    {
        [Int]
        public int Id { get; set; }
        public double? SizePrice { get; set; }
        public string? Size { get; set; }
    }
}
