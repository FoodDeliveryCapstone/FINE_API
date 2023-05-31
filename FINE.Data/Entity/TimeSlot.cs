using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class TimeSlot
{
    public int Id { get; set; }

    public int CampusId { get; set; }

    public TimeSpan ArriveTime { get; set; }

    public TimeSpan CheckoutTime { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public bool? ShowOnHome { get; set; }

    public string? Description { get; set; }

    public virtual Campus Campus { get; set; } = null!;

    public virtual ICollection<Menu> Menus { get; set; } = new List<Menu>();

    public virtual ICollection<ProductCollectionTimeSlot> ProductCollectionTimeSlots { get; set; } = new List<ProductCollectionTimeSlot>();
}
