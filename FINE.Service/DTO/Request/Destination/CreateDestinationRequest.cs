namespace FINE.Service.DTO.Request.Destination
{
    public class CreateDestinationRequest
    {
        public string Name { get; set; } = null!;

        public string Code { get; set; } = null!;

        public string Lat { get; set; } = null!;

        public string Long { get; set; } = null!;
    }
}
