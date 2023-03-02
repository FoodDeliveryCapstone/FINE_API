using FINE.Service.Commons;

namespace FINE.Service.DTO.Request.SystemCategory
{
    public class CreateSystemCategoryRequest
    {
        public string? CategoryCode { get; set; }

        public string? CategoryName { get; set; }

        public string? ImageUrl { get; set; }

        public bool ShowOnHome { get; set; }
    }
}
