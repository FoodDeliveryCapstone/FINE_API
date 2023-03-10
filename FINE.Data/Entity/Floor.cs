using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Floor
{
    public int Id { get; set; }

    public int Number { get; set; }

    public int CampusId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual Campus Campus { get; set; } = null!;

    public virtual ICollection<Room> Rooms { get; } = new List<Room>();
}
