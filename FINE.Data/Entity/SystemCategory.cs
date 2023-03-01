using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class SystemCategory
{
    public int Id { get; set; }

    public string CategoryCode { get; set; } = null!;

    public string CategoryName { get; set; } = null!;

    public string? ImageUrl { get; set; }

    public bool ShowOnHome { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual ICollection<Product> Products { get; } = new List<Product>();
}
