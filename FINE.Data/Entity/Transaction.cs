using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Transaction
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    public double Amount { get; set; }

    public string? Notes { get; set; }

    public bool IsIncrease { get; set; }

    public int Type { get; set; }

    public int? Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;
}
