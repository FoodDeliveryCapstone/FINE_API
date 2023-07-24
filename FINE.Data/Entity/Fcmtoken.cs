using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Fcmtoken
{
    public Guid Id { get; set; }

    public Guid? CustomerId { get; set; }

    public string Token { get; set; } = null!;

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual Customer? Customer { get; set; }
}
