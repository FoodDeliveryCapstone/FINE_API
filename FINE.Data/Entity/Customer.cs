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

    public virtual ICollection<Fcmtoken> Fcmtokens { get; set; } = new List<Fcmtoken>();

    public virtual ICollection<MembershipCard> MembershipCards { get; set; } = new List<MembershipCard>();

    public virtual ICollection<Notify> Notifies { get; set; } = new List<Notify>();

    public virtual ICollection<OrderFeedback> OrderFeedbacks { get; set; } = new List<OrderFeedback>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<ParticipationOrderDetail> ParticipationOrderDetails { get; set; } = new List<ParticipationOrderDetail>();

    public virtual ICollection<ParticipationOrder> ParticipationOrders { get; set; } = new List<ParticipationOrder>();

    public virtual ICollection<Staff> Staff { get; set; } = new List<Staff>();

    public virtual UniversityInfo UniInfo { get; set; } = null!;
}
