using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Customer
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string CustomerCode { get; set; } = null!;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public string ImageUrl { get; set; } = null!;

    public int UniversityId { get; set; }

    public int UniInfoId { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual ICollection<Fcmtoken> Fcmtokens { get; } = new List<Fcmtoken>();

    public virtual ICollection<MembershipCard> MembershipCards { get; } = new List<MembershipCard>();

    public virtual ICollection<Notify> Notifies { get; } = new List<Notify>();

    public virtual ICollection<OrderFeedback> OrderFeedbacks { get; } = new List<OrderFeedback>();

    public virtual ICollection<Order> Orders { get; } = new List<Order>();

    public virtual ICollection<ParticipationOrderDetail> ParticipationOrderDetails { get; } = new List<ParticipationOrderDetail>();

    public virtual ICollection<ParticipationOrder> ParticipationOrders { get; } = new List<ParticipationOrder>();

    public virtual ICollection<Staff> Staff { get; } = new List<Staff>();

    public virtual UniversityInfo UniInfo { get; set; } = null!;
}
