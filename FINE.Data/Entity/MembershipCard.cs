using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class MembershipCard
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public string CardCode { get; set; } = null!;

    public int CampusId { get; set; }

    public int Type { get; set; }

    public string PhysicalCardCode { get; set; } = null!;

    public bool Active { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

    public virtual Customer Customer { get; set; } = null!;
}
