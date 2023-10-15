using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Request.Station
{
    public class CreateStationRequest
    {
        public string Name { get; set; } = null!;

        public string Code { get; set; } = null!;

        public string AreaCode { get; set; } = null!;

        public Guid FloorId { get; set; }
    }
}
