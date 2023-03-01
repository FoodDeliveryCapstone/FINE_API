using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class StaffReport
{
    public int Id { get; set; }

    public int StaffId { get; set; }

    public double? TotalRestaurantCost { get; set; }

    public int? TotalOrder { get; set; }

    public int? TotalProduct { get; set; }

    public double? DayIncome { get; set; }

    public string? Note { get; set; }

    public DateTime? CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual Staff Staff { get; set; } = null!;
}
