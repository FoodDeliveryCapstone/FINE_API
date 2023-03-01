using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Room
{
    public int Id { get; set; }

    public int RoomNumber { get; set; }

    public int FloorNumber { get; set; }

    public int AreaId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Area Area { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; } = new List<Order>();
}
