using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Order
{
    public int Id { get; set; }

    public int? GeneralOrderId { get; set; }

    public string OrderCode { get; set; } = null!;

    public int? CustomerId { get; set; }

    public string DeliveryPhone { get; set; } = null!;

    public DateTime CheckInDate { get; set; }

    public double TotalAmount { get; set; }

    public double? Discount { get; set; }

    public double FinalAmount { get; set; }

    public double? ShippingFee { get; set; }

    public int? OrderStatus { get; set; }

    public int? OrderType { get; set; }

    public int TimeSlotId { get; set; }

    public int RoomId { get; set; }

    public int? StoreId { get; set; }

    public bool? IsConfirm { get; set; }

    public bool? IsPartyMode { get; set; }

    public int? ShipperId { get; set; }

    public int ItemQuantity { get; set; }

    public string? Note { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Order? GeneralOrder { get; set; }

    public virtual ICollection<Order> InverseGeneralOrder { get; } = new List<Order>();

    public virtual ICollection<OrderDetail> OrderDetails { get; } = new List<OrderDetail>();

    public virtual ICollection<OrderFeedback> OrderFeedbacks { get; } = new List<OrderFeedback>();

    public virtual ICollection<ParticipationOrder> ParticipationOrders { get; } = new List<ParticipationOrder>();

    public virtual ICollection<Payment> Payments { get; } = new List<Payment>();

    public virtual Room Room { get; set; } = null!;

    public virtual Staff? Shipper { get; set; }

    public virtual Store? Store { get; set; }
}
