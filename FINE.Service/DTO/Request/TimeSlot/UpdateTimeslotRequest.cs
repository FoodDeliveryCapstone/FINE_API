using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Request.TimeSlot
{
    public class UpdateTimeslotRequest
    {
        public int CampusId { get; set; }
        public DateTime ArriveTime { get; set; }
        public DateTime CheckoutTime { get; set; }
        public bool IsActive { get; set; }
    }
}
