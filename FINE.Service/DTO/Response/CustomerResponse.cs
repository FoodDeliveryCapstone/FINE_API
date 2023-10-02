using FINE.Service.Attributes;

namespace FINE.Service.DTO.Response
{
    public class CustomerResponse
    {
        public Guid? Id { get; set; }

        public string? Name { get; set; } = null!;

        public string? CustomerCode { get; set; } = null!;

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public double? Balance { get; set; }

        public double? Point { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string? ImageUrl { get; set; } = null!;

        public DateTime? CreateAt { get; set; }

        public DateTime? UpdateAt { get; set; }
    }
}
