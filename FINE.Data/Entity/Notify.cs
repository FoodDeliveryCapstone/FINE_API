using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Notify
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public bool? IsRead { get; set; }

    public bool? Active { get; set; }

    public DateTime? CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public string? Metadata { get; set; }

    public virtual Customer Customer { get; set; } = null!;
}
