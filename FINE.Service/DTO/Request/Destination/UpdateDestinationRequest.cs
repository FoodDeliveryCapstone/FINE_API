namespace FINE.Service.DTO.Request.Destination
{
    public class UpdateDestinationRequest
    {
        public string Name { get; set; } = null!;

        public string Lat { get; set; } = null!;

        public string Long { get; set; } = null!;

        public bool IsActive { get; set; }

    }
}
