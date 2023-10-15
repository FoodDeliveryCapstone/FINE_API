using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Request.Box
{
    public class UpdateBoxRequest
    {
        public Guid StationId { get; set; }

        public string Code { get; set; } = null!;

        public bool IsActive { get; set; }

        public bool IsHeat { get; set; }
    }
}
