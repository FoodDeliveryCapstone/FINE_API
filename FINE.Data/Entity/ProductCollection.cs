using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class ProductCollection
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? BannerUrl { get; set; }

    public int StoreId { get; set; }

    public int Type { get; set; }

    public int? Position { get; set; }

    public bool Active { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual ICollection<ProductCollectionTimeSlot> ProductCollectionTimeSlots { get; set; } = new List<ProductCollectionTimeSlot>();

    public virtual ICollection<ProductionCollectionItem> ProductionCollectionItems { get; set; } = new List<ProductionCollectionItem>();
}
