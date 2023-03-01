using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Area
{
    public int Id { get; set; }

    public int CampusId { get; set; }

    public string Name { get; set; } = null!;

    public string AreaCode { get; set; } = null!;

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual Campus Campus { get; set; } = null!;

    public virtual ICollection<Room> Rooms { get; } = new List<Room>();
}
