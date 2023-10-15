using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class StationResponse
    {
        public Guid? Id { get; set; }

        public string? Name { get; set; } = null!;

        public string? Code { get; set; } = null!;

        public string? AreaCode { get; set; } = null!;

        public Guid? FloorId { get; set; }
        public bool IsAvailable { get; set; }

        public bool? IsActive { get; set; }

        public DateTime? CreateAt { get; set; }

        public DateTime? UpdateAt { get; set; }
    }
}
