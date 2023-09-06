using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;

namespace FINE.Service.DTO.Request.Box
{
    public class AddOrderToBoxRequest
    {
        public Guid OrderId { get; set; }
        public Guid BoxId { get; set; }
    }
}