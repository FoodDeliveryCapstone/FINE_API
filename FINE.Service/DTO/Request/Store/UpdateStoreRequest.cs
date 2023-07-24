namespace FINE.Service.DTO.Request.Store
{
    public class UpdateStoreRequest
    {
        public Guid DestinationId { get; set; }

        public string StoreName { get; set; } = null!;

        public string? ImageUrl { get; set; }

        public string? ContactPerson { get; set; }

        public bool IsActive { get; set; }
    }
}
