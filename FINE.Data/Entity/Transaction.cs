using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Transaction
{
    public int Id { get; set; }

    public int AccountId { get; set; }

    public double Amount { get; set; }

    public string? Notes { get; set; }

    public bool IsIncrease { get; set; }

    public int Status { get; set; }

    public int BrandId { get; set; }

    public int Type { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;
}
