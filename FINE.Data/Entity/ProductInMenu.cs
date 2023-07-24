using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class ProductInMenu
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Guid MenuId { get; set; }

    public double Price { get; set; }

    public bool IsActive { get; set; }

    public int Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Menu Menu { get; set; } = null!;

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual Product Product { get; set; } = null!;
}
