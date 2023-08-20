using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Party
{
    public Guid Id { get; set; }

    public Guid? OrderId { get; set; }

    public Guid CustomerId { get; set; }

    public string PartyCode { get; set; } = null!;

    public int PartyType { get; set; }

    public int Status { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual Order? Order { get; set; }
}
