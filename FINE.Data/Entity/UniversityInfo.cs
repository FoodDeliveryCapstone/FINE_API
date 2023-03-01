using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class UniversityInfo
{
    public int Id { get; set; }

    public int UniversityId { get; set; }

    public string Domain { get; set; } = null!;

    public string Role { get; set; } = null!;

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual ICollection<Customer> Customers { get; } = new List<Customer>();

    public virtual University University { get; set; } = null!;
}
