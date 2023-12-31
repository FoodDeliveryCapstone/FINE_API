﻿using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class ProductAttribute
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string? Size { get; set; }

    public double Price { get; set; }

    public bool IsActive { get; set; }

    public int RotationType { get; set; }

    public double Height { get; set; }

    public double Width { get; set; }

    public double Length { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<ProductInMenu> ProductInMenus { get; set; } = new List<ProductInMenu>();
}
