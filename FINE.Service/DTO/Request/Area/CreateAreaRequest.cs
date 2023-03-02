namespace FINE.Service.DTO.Request.Area
{
    public class CreateAreaRequest
    {
        public string Name { get; set; } = null!;
        public int CampusId { get; set; }
        public string AreaCode { get; set; } = null!;
    }
}
