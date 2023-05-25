using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class ParticipationOrder
{
    public int Id { get; set; }

    public int GeneralOrderId { get; set; }

    public string GeneralOrderCode { get; set; } = null!;

    public int CustomerId { get; set; }

    public double TotalAmount { get; set; }

    public double? Discount { get; set; }

    public double FinalAmount { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual Order GeneralOrder { get; set; } = null!;

    public virtual ICollection<ParticipationOrderDetail> ParticipationOrderDetails { get; set; } = new List<ParticipationOrderDetail>();
}
