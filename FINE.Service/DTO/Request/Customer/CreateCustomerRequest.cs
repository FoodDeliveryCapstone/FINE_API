using FINE.Service.Attributes;

namespace FINE.Service.DTO.Request.Customer
{
    public class CreateCustomerRequest
    {
        [String]
        public string Name { get; set; } = null!;

        public string CustomerCode { get; set; } = null!;

        public string? Email { get; set; }

        public string Phone { get; set; } = null!;

        public DateTime? DateOfBirth { get; set; }

        public string ImageUrl { get; set; } = null!;

        public int UniversityId { get; set; }

        public int UniInfoId { get; set; }

        public DateTime CreateAt { get; set; }
    }
}
