using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Notify
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public bool IsRead { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public string? Metadata { get; set; }

    public virtual Customer Customer { get; set; } = null!;
}
