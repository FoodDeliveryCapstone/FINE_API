using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Request.Menu
{
    public class UpdateMenuRequest
    {
        public int TimeSlotId { get; set; }

        public string MenuName { get; set; } = null!;
        public string? ImgUrl { get; set; }

        public bool IsActive { get; set; }

    }
}
