using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class StationReport
{
    public Guid Id { get; set; }

    public string StaffId { get; set; } = null!;

    public Guid BoxId { get; set; }

    public int ErrorType { get; set; }

    public int Status { get; set; }

    public string? Message { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual Box Box { get; set; } = null!;

    public virtual Staff Staff { get; set; } = null!;
}
