using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Request.Box
{
    public class CreateBoxRequest
    {
        public Guid StationId { get; set; }

        public string Code { get; set; } = null!;

        public bool IsHeat { get; set; }
    }
}
