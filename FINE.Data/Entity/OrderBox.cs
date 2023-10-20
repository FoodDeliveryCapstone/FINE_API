using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class OrderBox
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid BoxId { get; set; }

    public string? Key { get; set; }

    public int Status { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual Box Box { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
