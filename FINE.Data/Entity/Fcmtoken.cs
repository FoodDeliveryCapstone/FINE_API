using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Fcmtoken
{
    public int Id { get; set; }

    public int? CustomerId { get; set; }

    public int? StaffId { get; set; }

    public string Token { get; set; } = null!;

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Staff? Staff { get; set; }
}
