using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Order
{
    public Guid Id { get; set; }

    public string OrderCode { get; set; } = null!;

    public int? CustomerId { get; set; }

    public DateTime CheckInDate { get; set; }

    public double TotalAmount { get; set; }

    public double FinalAmount { get; set; }

    public double TotalOtherAmount { get; set; }

    public int OrderStatus { get; set; }

    public int OrderType { get; set; }

    public Guid TimeSlotId { get; set; }

    public Guid StoreId { get; set; }

    public Guid StationId { get; set; }

    public bool IsConfirm { get; set; }

    public bool IsPartyMode { get; set; }

    public int ItemQuantity { get; set; }

    public string? Note { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<OtherAmount> OtherAmounts { get; set; } = new List<OtherAmount>();

    public virtual ICollection<Party> Parties { get; set; } = new List<Party>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Store Store { get; set; } = null!;

    public virtual TimeSlot TimeSlot { get; set; } = null!;
}
