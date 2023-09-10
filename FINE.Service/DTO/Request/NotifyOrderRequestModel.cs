using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;

namespace FINE.Service.DTO.Request
{
    public class NotifyOrderRequestModel
    {
        public string OrderCode { get; set; }
        public Guid CustomerId { get; set; }
        public OrderStatusEnum? OrderStatus { get; set; }
    }
    public class NotifyRequestModel
    {
        public Guid CustomerId { get; set; }
        public string Title { get; set; }
        public NotifyTypeEnum Type { get; set; }
    }
}
