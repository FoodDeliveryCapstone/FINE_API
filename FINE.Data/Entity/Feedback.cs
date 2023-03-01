using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Feedback
{
    public int Id { get; set; }

    public string? FbContent { get; set; }

    public int? CustomerId { get; set; }

    public bool? IsApproved { get; set; }

    public bool? Active { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
