using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Staff
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Username { get; set; } = null!;

    public byte[] Password { get; set; } = null!;

    public int RoleType { get; set; }

    public Guid? StoreId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual ICollection<StationReport> StationReports { get; set; } = new List<StationReport>();

    public virtual Store? Store { get; set; }
}
