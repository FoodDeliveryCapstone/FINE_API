using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class OrderDetail
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid ProductInMenuId { get; set; }

    public string ProductCode { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public double UnitPrice { get; set; }

    public int Quantity { get; set; }

    public double TotalAmount { get; set; }

    public double? Discount { get; set; }

    public double FinalAmount { get; set; }

    public string? Note { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual ProductInMenu ProductInMenu { get; set; } = null!;
}
