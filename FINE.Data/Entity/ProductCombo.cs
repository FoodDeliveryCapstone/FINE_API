using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class ProductCombo
{
    public int Id { get; set; }

    public string ComboCode { get; set; } = null!;

    public string CombineName { get; set; } = null!;

    public double Price { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<ParticipationOrderDetail> ParticipationOrderDetails { get; set; } = new List<ParticipationOrderDetail>();

    public virtual ICollection<ProductComboItem> ProductComboItems { get; set; } = new List<ProductComboItem>();
}
