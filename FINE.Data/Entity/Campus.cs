using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Campus
{
    public int Id { get; set; }

    public int UniversityId { get; set; }

    public string Name { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string Code { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual ICollection<Area> Areas { get; } = new List<Area>();

    public virtual ICollection<Staff> Staff { get; } = new List<Staff>();

    public virtual ICollection<Store> Stores { get; } = new List<Store>();

    public virtual ICollection<TimeSlot> TimeSlots { get; } = new List<TimeSlot>();

    public virtual University University { get; set; } = null!;
}
