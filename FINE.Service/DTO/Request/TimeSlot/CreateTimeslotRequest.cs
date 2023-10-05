using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Request.TimeSlot
{
    public class CreateTimeslotRequest
    {
        public Guid DestinationId { get; set; }
        public TimeOnly CloseTime { get; set; }

        public TimeOnly ArriveTime { get; set; }

        public TimeOnly CheckoutTime { get; set; }
    }
}
