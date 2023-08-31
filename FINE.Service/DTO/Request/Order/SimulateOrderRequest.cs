using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;

namespace FINE.Service.DTO.Request.Order
{
    public class SimulateRequest
    {
        public string TimeSlotId { get; set; }
        public SimulateSingleOrderRequest? SingleOrder { get; set; }
        public SimulateCoOrderOrderRequest? CoOrderOrder { get; set; }
    }

    public class SimulateSingleOrderRequest
    {
        public int? OrderPerPerson { get; set; }
        public int? TotalCustomer { get; set; }
        public int? TotalOrderSuccess { get; set; }       
    }

    public class SimulateCoOrderOrderRequest
    {
        public int? TotalOrder { get; set; }
        public int? CustomerEach { get; set; }
        public int? TotalOrderSuccess { get; set; }
    }
}
