using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class OtherAmount
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public double Amount { get; set; }

    public int Type { get; set; }

    public string? Note { get; set; }

    public virtual Order Order { get; set; } = null!;
}
