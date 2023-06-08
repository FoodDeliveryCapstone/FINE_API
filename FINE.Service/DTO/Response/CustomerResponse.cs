using FINE.Service.Attributes;

namespace FINE.Service.DTO.Response
{
    public class CustomerResponse
    {
        [Int]
        public int? Id { get; set; }
        [String]
        public string? Name { get; set; } = null!;
        [String]
        public string? CustomerCode { get; set; } = null!;
        [String]
        public string? Email { get; set; }
        [String]
        public string? Phone { get; set; } = null!;

        public DateTime? DateOfBirth { get; set; }

        public string? ImageUrl { get; set; } = null!;

        public int? UniversityId { get; set; }

        public int? UniInfoId { get; set; }

        public DateTime? CreateAt { get; set; }

        public DateTime? UpdateAt { get; set; }
    }
}
