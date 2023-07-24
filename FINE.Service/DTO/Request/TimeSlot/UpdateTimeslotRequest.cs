using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Request.TimeSlot
{
    public class UpdateTimeslotRequest
    {
        public int DestinationId { get; set; }
        public TimeOnly ArriveTime { get; set; }
        public TimeOnly CheckoutTime { get; set; }
        public bool IsActive { get; set; }
    }
}
