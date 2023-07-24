using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Product
{
    public Guid Id { get; set; }

    public Guid StoreId { get; set; }

    public Guid CategoryId { get; set; }

    public string ProductCode { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public int ProductType { get; set; }

    public string ImageUrl { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<ProductAttribute> ProductAttributes { get; set; } = new List<ProductAttribute>();

    public virtual ICollection<ProductInMenu> ProductInMenus { get; set; } = new List<ProductInMenu>();

    public virtual Store Store { get; set; } = null!;
}
