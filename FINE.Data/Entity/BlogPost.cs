using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class BlogPost
{
    public int Id { get; set; }

    public int StoreId { get; set; }

    public string Title { get; set; } = null!;

    public string BlogContent { get; set; } = null!;

    public string? ImageUrl { get; set; }

    public bool Active { get; set; }

    public bool? IsDialog { get; set; }

    public string? Metadata { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual Store Store { get; set; } = null!;
}
