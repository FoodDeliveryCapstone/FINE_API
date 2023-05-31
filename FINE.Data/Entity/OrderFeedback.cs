using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class OrderFeedback
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int CustomerId { get; set; }

    public string? OrderFbContent { get; set; }

    public int Rating { get; set; }

    public string? Description { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
