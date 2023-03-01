using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class CategoryStoreItem
{
    public int Id { get; set; }

    public int StoreCategoryId { get; set; }

    public int ProductId { get; set; }

    public double Price { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual StoreCategory StoreCategory { get; set; } = null!;
}
