using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Box
{
    public Guid Id { get; set; }

    public Guid StationId { get; set; }

    public string Code { get; set; } = null!;

    public bool IsActive { get; set; }

    public bool IsHeat { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual Station Station { get; set; } = null!;

    public virtual ICollection<StationReport> StationReports { get; set; } = new List<StationReport>();
}
