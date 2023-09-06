using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;

namespace FINE.Service.DTO.Response
{
    public class OrderBoxResponse
    {
        public Guid OrderId { get; set; }
        public Guid BoxId { get; set; }
        public OrderBoxStatusEnum Status { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }
    }
}