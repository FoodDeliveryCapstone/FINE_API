using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Product
{
    public int Id { get; set; }

    public int? GeneralProductId { get; set; }

    public string? ProductCode { get; set; }

    public string ProductName { get; set; } = null!;

    public int CategoryId { get; set; }

    public int? ProductType { get; set; }

    public int StoreId { get; set; }

    public double BasePrice { get; set; }

    public double? SizePrice { get; set; }

    public string? Size { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual SystemCategory Category { get; set; } = null!;

    public virtual ICollection<CategoryStoreItem> CategoryStoreItems { get; } = new List<CategoryStoreItem>();

    public virtual Product? GeneralProduct { get; set; }

    public virtual ICollection<Product> InverseGeneralProduct { get; } = new List<Product>();

    public virtual ICollection<ProductComboItem> ProductComboItems { get; } = new List<ProductComboItem>();

    public virtual ICollection<ProductInMenu> ProductInMenus { get; } = new List<ProductInMenu>();

    public virtual ICollection<ProductionCollectionItem> ProductionCollectionItems { get; } = new List<ProductionCollectionItem>();

    public virtual Store Store { get; set; } = null!;
}
