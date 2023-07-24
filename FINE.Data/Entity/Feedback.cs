using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Feedback
{
    public int Id { get; set; }

    public Guid OrderId { get; set; }

    public string? Content { get; set; }

    public bool? IsApproved { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;
}
