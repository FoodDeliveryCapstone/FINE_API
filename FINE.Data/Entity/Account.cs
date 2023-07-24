using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Account
{
    public Guid Id { get; set; }

    public Guid? CustomerId { get; set; }

    public string AccountCode { get; set; } = null!;

    public string AccountName { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public decimal Balance { get; set; }

    public int Type { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
