using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Station
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string AreaCode { get; set; } = null!;

    public Guid FloorId { get; set; }

    public bool IsAvailable { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual ICollection<Box> Boxes { get; set; } = new List<Box>();

    public virtual Floor Floor { get; set; } = null!;

    public virtual ICollection<Staff> Staff { get; set; } = new List<Staff>();
}
