using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Request
{
    public class NotifyRequestModel
    {
        public string OrderCode { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public int? OrderStatus { get; set; }
        public int Type { get; set; }
        public string Description { get; set; }
        public string Title { get; set; }
    }
}
