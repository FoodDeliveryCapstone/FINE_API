using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Payment
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int PaymentType { get; set; }

    public int Status { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public string? Note { get; set; }

    public virtual Order Order { get; set; } = null!;
}
