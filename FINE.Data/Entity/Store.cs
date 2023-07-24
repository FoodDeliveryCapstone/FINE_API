using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Store
{
    public Guid Id { get; set; }

    public Guid DestinationId { get; set; }

    public string StoreName { get; set; } = null!;

    public string? ImageUrl { get; set; }

    public string? ContactPerson { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Destination Destination { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<Staff> Staff { get; set; } = new List<Staff>();
}
