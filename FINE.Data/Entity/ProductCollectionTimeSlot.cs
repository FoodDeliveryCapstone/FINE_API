using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class ProductCollectionTimeSlot
{
    public int Id { get; set; }

    public int ProductCollectionId { get; set; }

    public int TimeSlotId { get; set; }

    public int Position { get; set; }

    public bool IsShownAtHome { get; set; }

    public bool Active { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ProductCollection ProductCollection { get; set; } = null!;

    public virtual TimeSlot TimeSlot { get; set; } = null!;
}
