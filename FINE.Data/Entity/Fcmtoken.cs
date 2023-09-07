using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Fcmtoken
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string Token { get; set; } = null!;

    public int? UserType { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }
}
