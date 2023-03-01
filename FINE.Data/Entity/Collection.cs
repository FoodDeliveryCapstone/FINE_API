using System;
using System.Collections.Generic;

namespace FINE.Data.Entity
{
    public partial class Collection
    {
        public Collection()
        {
            Products = new HashSet<Product>();
        }

        public int Id { get; set; }
        public int StoreId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string ImageUrl { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual Store Store { get; set; } = null!;
        public virtual ICollection<Product> Products { get; set; }
    }
}
