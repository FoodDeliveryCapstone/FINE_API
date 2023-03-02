using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Request.Noti
{
    public class UpdateNotifyRequest
    {
        public int CustomerId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public bool? IsRead { get; set; }
        public bool? Active { get; set; }
    }
}
