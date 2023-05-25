using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class StoreCategory
{
    public int Id { get; set; }

    public int StoreId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<CategoryStoreItem> CategoryStoreItems { get; set; } = new List<CategoryStoreItem>();

    public virtual Store Store { get; set; } = null!;
}
