using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class TimeSlot
{
    public Guid Id { get; set; }

    public Guid DestinationId { get; set; }

    public TimeSpan ArriveTime { get; set; }

    public TimeSpan CheckoutTime { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public string? Description { get; set; }

    public virtual Destination Destination { get; set; } = null!;

    public virtual ICollection<Menu> Menus { get; set; } = new List<Menu>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
