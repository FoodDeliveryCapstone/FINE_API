using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Floor
{
    public Guid Id { get; set; }

    public Guid DestionationId { get; set; }

    public int Number { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual Destination Destionation { get; set; } = null!;

    public virtual ICollection<Station> Stations { get; set; } = new List<Station>();
}
