namespace FINE.Service.DTO.Response;

public class TimslotResponse
{
    public int Id { get; set; }

    public int CampusId { get; set; }

    public TimeSpan ArriveTime { get; set; }

    public TimeSpan CheckoutTime { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }
}