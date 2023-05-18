using FINE.Service.Commons;

namespace FINE.Service.DTO.Response
{
    public class StoreCategoryResponse
    {
        public int? Id { get; set; }

        public int? StoreId { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
