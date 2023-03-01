using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class University
{
    public int Id { get; set; }

    public string UniName { get; set; } = null!;

    public string UniCode { get; set; } = null!;

    public string? ContactName { get; set; }

    public string? ContactEmail { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual ICollection<Campus> Campuses { get; } = new List<Campus>();

    public virtual ICollection<UniversityInfo> UniversityInfos { get; } = new List<UniversityInfo>();
}
