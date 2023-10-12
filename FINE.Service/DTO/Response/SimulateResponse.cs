using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class SimulateResponse
    {
        public TimeSlotOrderResponse Timeslot { get; set; }
        public SimulateSingleOrderResponse SingleOrderResult { get; set; }
        public SimulateCoOrderResponse CoOrderOrderResult { get; set; }
    }

    public class SimulateSingleOrderResponse
    {
        public List<OrderSuccess> OrderSuccess { get; set; }
        public List<OrderFailed> OrderFailed { get; set; }

    }
    public class OrderFailed
    {
        public string OrderCode { get; set; }
        public CustomerOrderResponse Customer { get; set; }
        public StatusViewModel Status { get; set; }
    }
    public class OrderSuccess
    {
        public Guid Id { get; set; }
        public string OrderCode { get; set; }
        public CustomerOrderResponse Customer { get; set; }
        public List<OrderSuccessOrderDetail> OrderDetails { get; set; }
    }

    public class SimulateCoOrderResponse
    {
        public List<OrderSuccess> OrderSuccess { get; set; }
        public List<OrderFailed> OrderFailed { get; set; }
    }
    public class SimulateOrderStatusResponse 
    {
        public string OrderCode { get; set; }
        public int ItemQuantity { get; set; }
        public string CustomerName { get; set; }
    }

    public class OrderSuccessOrderDetail
    {
        public Guid StoreId { get; set; }
        public string? ProductName { get; set; } = null!;
        public int Quantity { get; set; }
    }

    public class SimulateOrderForStaffResponse
    {
        public List<SimulateOrderForStaffSuccess> OrderSuccess { get; set; }
        public List<SimulateOrderForStaffFailed> OrderFailed { get; set; }
    }

    public class SimulateOrderForStaffSuccess
    {
        public Guid? StoreId { get; set; }
        public string StaffName { get; set; }
        public string? ProductName { get; set; } = null!;
        public int Quantity { get; set; }
    }

    public class SimulateOrderForStaffFailed
    {
        public Guid? StoreId { get; set; }
        public string StaffName { get; set; }
        public string? ProductName { get; set; } = null!;
        public int Quantity { get; set; }
        public StatusViewModel Status { get; set; }
    }
}
