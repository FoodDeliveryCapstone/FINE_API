using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class ProductInMenu
{
    public int Id { get; set; }

    public int? ProductId { get; set; }

    public int? MenuId { get; set; }

    public int? StoreId { get; set; }

    public double? Price { get; set; }

    public bool IsAvailable { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Menu? Menu { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; } = new List<OrderDetail>();

    public virtual ICollection<ParticipationOrderDetail> ParticipationOrderDetails { get; } = new List<ParticipationOrderDetail>();

    public virtual Product? Product { get; set; }
}
