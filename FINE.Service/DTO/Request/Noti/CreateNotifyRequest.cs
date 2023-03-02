using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Request.Noti
{
    public class CreateNotifyRequest
    {
        public int CustomerId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
    }
}
