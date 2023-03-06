namespace FINE.Service.DTO.Request.Campus
{
    public class CreateCampusRequest
    {
        public int UniversityId { get; set; }

        public string Name { get; set; } = null!;

        public string Address { get; set; } = null!;

        public string Code { get; set; } = null!;
    }
}
