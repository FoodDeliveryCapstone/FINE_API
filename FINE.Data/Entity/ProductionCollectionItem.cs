using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class ProductionCollectionItem
{
    public int Id { get; set; }

    public int ProductCollectionId { get; set; }

    public int ProductId { get; set; }

    public bool Active { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual ProductCollection ProductCollection { get; set; } = null!;
}
