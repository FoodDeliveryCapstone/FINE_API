namespace FINE.Service.DTO.Response;

public class RoomResponse
{
    public int Id { get; set; }

    public int Number { get; set; }

    public int FloorId { get; set; }

    public int AreaId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}