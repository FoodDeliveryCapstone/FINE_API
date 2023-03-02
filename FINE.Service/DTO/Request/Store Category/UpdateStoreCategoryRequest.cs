namespace FINE.Service.DTO.Request.Store_Category
{
    public class UpdateStoreCategoryRequest
    {
        public int StoreId { get; set; }

        public string Name { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; }

    }
}
