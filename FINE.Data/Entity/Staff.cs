using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Staff
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Username { get; set; } = null!;

    public byte[] Password { get; set; } = null!;

    public int RoleType { get; set; }

    public int? CampusId { get; set; }

    public int? CustomerId { get; set; }

    public bool? IsAvailable { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual Campus? Campus { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<Fcmtoken> Fcmtokens { get; } = new List<Fcmtoken>();

    public virtual ICollection<Order> Orders { get; } = new List<Order>();

    public virtual ICollection<StaffReport> StaffReports { get; } = new List<StaffReport>();
}
