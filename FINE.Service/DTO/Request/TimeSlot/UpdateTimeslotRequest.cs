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
        public TimeSpan ArriveTime { get; set; }
        public TimeSpan CheckoutTime { get; set; }
        public bool IsActive { get; set; }
    }
}
