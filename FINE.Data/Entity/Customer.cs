using System;
using System.Collections.Generic;

namespace FINE.Data.Entity;

public partial class Customer
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? CustomerCode { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

    public virtual ICollection<Notify> Notifies { get; set; } = new List<Notify>();

    public virtual ICollection<Party> Parties { get; set; } = new List<Party>();
}
