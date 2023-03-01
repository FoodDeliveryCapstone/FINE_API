using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Menu
{
    public int Id { get; set; }

    public int TimeSlotId { get; set; }

    public string MenuName { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual ICollection<ProductInMenu> ProductInMenus { get; } = new List<ProductInMenu>();

    public virtual TimeSlot TimeSlot { get; set; } = null!;
}
