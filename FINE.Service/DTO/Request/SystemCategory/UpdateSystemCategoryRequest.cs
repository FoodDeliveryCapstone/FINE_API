using FINE.Service.Attributes;

namespace FINE.Service.DTO.Request.SystemCategory
{
    public class UpdateSystemCategoryRequest
    {
        public string CategoryCode { get; set; }

        public string CategoryName { get; set; } 

        public string? ImageUrl { get; set; }

        public bool ShowOnHome { get; set; }

    }
}
