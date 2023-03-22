using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Store
{
    public int Id { get; set; }

    public int CampusId { get; set; }

    public string? StoreName { get; set; }

    public string? ImageUrl { get; set; }

    public string? ContactPerson { get; set; }

    public bool IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<BlogPost> BlogPosts { get; } = new List<BlogPost>();

    public virtual Campus Campus { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; } = new List<Order>();

    public virtual ICollection<Product> Products { get; } = new List<Product>();

    public virtual ICollection<StoreCategory> StoreCategories { get; } = new List<StoreCategory>();
}
