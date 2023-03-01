using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class ProductComboItem
{
    public int Id { get; set; }

    public int ComboId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public double Price { get; set; }

    public virtual ProductCombo Combo { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
