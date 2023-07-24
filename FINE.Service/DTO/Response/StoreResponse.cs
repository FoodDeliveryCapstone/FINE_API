namespace FINE.Service.DTO.Response
{
    public class StoreResponse
    {
        public Guid? Id { get; set; }

        public Guid? DestinationId { get; set; }

        public string? StoreName { get; set; } = null!;

        public string? ImageUrl { get; set; }

        public string? ContactPerson { get; set; }

        public bool? IsActive { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

    }
}
