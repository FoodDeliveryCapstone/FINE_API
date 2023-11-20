using FINE.Service.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class TimeSlotResponse
    {
        public CubeModel BoxSize { get; set; }
        public int MaxQuantityInBox { get; set; }
        public List<ListTimeslotResponse> ListTimeslotResponse { get; set; }
    }
    public class ListTimeslotResponse
    {
        public Guid Id { get; set; }

        public TimeSpan CloseTime { get; set; }

        public TimeSpan ArriveTime { get; set; }

        public TimeSpan CheckoutTime { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreateAt { get; set; }

        public DateTime? UpdateAt { get; set; }

    }
}
