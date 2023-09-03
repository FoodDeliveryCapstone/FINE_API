using FINE.Service.Attributes;

namespace FINE.Service.DTO.Request.Customer
{
    public class UpdateCustomerRequest
    {
        public string? Name { get; set; } = null!;

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string? ImageUrl { get; set; } = null!;
    }
}
