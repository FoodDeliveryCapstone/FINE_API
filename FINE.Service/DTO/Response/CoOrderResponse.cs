using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class CoOrderResponse
    {
        public Guid Id { get; set; }

        public string PartyCode { get; set; }

        public int PartyType { get; set; }

        public int OrderType { get; set; }

        public bool IsPayment { get; set; }

        public bool IsActive { get; set; }

        public TimeSlotOrderResponse TimeSlot { get; set; }

        public List<CoOrderPartyCard> PartyOrder { get; set; }
    }

    public class CoOrderPartyCard
    {
        public CustomerCoOrderResponse Customer { get; set; }

        public double TotalAmount { get; set; }

        public int ItemQuantity { get; set; }

        public List<CoOrderDetailResponse> OrderDetails { get; set; }
    }

    public class CoOrderDetailResponse
    {
        public Guid ProductInMenuId { get; set; }

        public Guid ProductId { get; set; }

        public string ProductName { get; set; } = null!;

        public double UnitPrice { get; set; }

        public int Quantity { get; set; }

        public double TotalAmount { get; set; }

        public string? Note { get; set; }
    }

    public class CustomerCoOrderResponse
    {
        public Guid? Id { get; set; }

        public string? Name { get; set; } = null!;

        public string? CustomerCode { get; set; } = null!;

        public bool IsAdmin { get; set; }

        public bool IsConfirm { get; set; }
    }
}
