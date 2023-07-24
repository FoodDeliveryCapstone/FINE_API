namespace FINE.Service.DTO.Response
{
    public class DestinationResponse
    {
        public Guid? Id { get; set; }

        public string? Name { get; set; } = null!;

        public string? Code { get; set; } = null!;

        public string? Lat { get; set; } = null!;

        public string? Long { get; set; } = null!;

        public bool? IsActive { get; set; }

        public DateTime? CreateAt { get; set; }

        public DateTime? UpdateAt { get; set; }

    }
}
