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
        public StatusViewModel Status { get; set; }
    }
    public class OrderSuccess
    {
        public Guid Id { get; set; }
        public string OrderCode { get; set; }
        public CustomerOrderResponse Customer { get; set; }
    }

    public class SimulateCoOrderResponse
    {
        public List<OrderSuccess> OrderSuccess { get; set; }
        public List<OrderFailed> OrderFailed { get; set; }
    }
}
