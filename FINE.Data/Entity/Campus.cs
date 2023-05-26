﻿using System;
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

    public virtual ICollection<Area> Areas { get; set; } = new List<Area>();

    public virtual ICollection<Floor> Floors { get; set; } = new List<Floor>();

    public virtual ICollection<Store> Stores { get; set; } = new List<Store>();

    public virtual ICollection<TimeSlot> TimeSlots { get; set; } = new List<TimeSlot>();

    public virtual University University { get; set; } = null!;
}
