using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Account
{
    public int Id { get; set; }

    public int MembershipCardId { get; set; }

    public string AccountCode { get; set; } = null!;

    public string AccountName { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime? FinishDate { get; set; }

    public decimal Balance { get; set; }

    public int Type { get; set; }

    public int CampusId { get; set; }

    public bool Active { get; set; }

    public virtual MembershipCard MembershipCard { get; set; } = null!;

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
