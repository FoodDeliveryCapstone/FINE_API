namespace FINE.Service.DTO.Response
{
    public class CampusResponse
    {
        public int Id { get; set; }

        public int UniversityId { get; set; }

        public string? Name { get; set; } 

        public string? Address { get; set; } 

        public string? Code { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreateAt { get; set; }

        public DateTime? UpdateAt { get; set; }

        // public int Id { get; set; }
        // public string? Name { get; set; }
        // public string? BrandName { get; set; }
        public string? EmailRoot { get; set; }
        // public DateTime CreateAt { get; set; }
        // public DateTime? UpdateAt { get; set; }

    }
}
