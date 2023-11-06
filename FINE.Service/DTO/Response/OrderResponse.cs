using FINE.Data.Entity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;

namespace FINE.Service.DTO.Response
{
    public class OrderResponse
    {
        public Guid Id { get; set; }

        public string OrderCode { get; set; } = null!;

        public CustomerOrderResponse Customer { get; set; }

        public DateTime CheckInDate { get; set; }

        public double TotalAmount { get; set; }

        public double FinalAmount { get; set; }

        public double TotalOtherAmount { get; set; }

        public List<OrderOtherAmount> OtherAmounts { get; set; }

        public int OrderStatus { get; set; }

        public int OrderType { get; set; }

        public TimeSlotOrderResponse TimeSlot { get; set; }

        public StationOrderResponse StationOrder { get; set; }

        public int BoxQuantity { get; set; }

        public int Point { get; set; }

        public bool IsConfirm { get; set; }

        public bool IsPartyMode { get; set; }

        public int ItemQuantity { get; set; }

        public string? Note { get; set; }

        public DateTime? UpdateAt { get; set; }

        public List<OrderDetailResponse> OrderDetails { get; set; }

        public List<string> BoxesCode { get; set; }

    }
    public class OrderDetailResponse
    {
        public Guid Id { get; set; }

        public Guid OrderId { get; set; }

        public Guid ProductInMenuId { get; set; }

        public Guid ProductId { get; set; }

        public Guid StoreId { get; set; }

        public string? ProductCode { get; set; } = null!;

        public string? ProductName { get; set; } = null!;

        public string? ImageUrl { get; set; }

        public double UnitPrice { get; set; }

        public int Quantity { get; set; }

        public double TotalAmount { get; set; }

        public double? Discount { get; set; }

        public double FinalAmount { get; set; }

        public string? Note { get; set; }
    }

    public class CustomerOrderResponse
    {
        public Guid? Id { get; set; }

        public string? Name { get; set; } = null!;

        public string? CustomerCode { get; set; } = null!;

        public string? Email { get; set; }

        public string? Phone { get; set; }
    }

    public class OrderOtherAmount
    {
        public Guid Id { get; set; }

        public Guid OrderId { get; set; }

        public double Amount { get; set; }

        public int Type { get; set; }
    }

    public class TimeSlotOrderResponse
    {
        public string? Id { get; set; }

        public TimeSpan? CloseTime { get; set; }

        public TimeSpan? ArriveTime { get; set; }

        public TimeSpan? CheckoutTime { get; set; }
    }

    public class StationOrderResponse
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;

        public string Code { get; set; } = null!;

        public string AreaCode { get; set; } = null!;

        public Guid FloorId { get; set; }
    }


    public class OrderResponseForCustomer
    {
        public Guid? Id { get; set; }

        public DateTime? CheckInDate { get; set; }

        public string? OrderCode { get; set; } 

        public double? FinalAmount { get; set; }

        public int? OrderStatus { get; set; }

        public int? OrderType { get; set; }

        public bool? IsPartyMode { get; set; }

        public int? ItemQuantity { get; set; }
    }

    public class CreateReOrderResponse
    {
        public List<string> ProductCannotAdd { get; set; }
        public OrderResponse OrderResponse { get; set; }
    }

    public class OrderForAdminResponse
    {
        public Guid Id { get; set; }
        public string? OrderCode { get; set; } = null!;

        public Guid CustomerId { get; set; }
        public DateTime? CheckInDate { get; set; }
        public double? FinalAmount { get; set; }

        public int? OrderStatus { get; set; }

        public int? OrderType { get; set; }

        public TimeSlotOrderResponse? TimeSlot { get; set; }

        public Guid StationId { get; set; }

        public bool? IsConfirm { get; set; }

        public bool? IsPartyMode { get; set; }

        public int? ItemQuantity { get; set; }

        public string? Note { get; set; }
        public double? RefundAmount { get; set; }
        public double? FinalAmountAfterRefund { get; set; }
        public string? RefundNote { get; set; }

        public List<OrderDetailResponse>? OrderDetails { get; set; }

    }
}
