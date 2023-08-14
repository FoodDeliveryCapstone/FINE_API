using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class BoxResponse
    {
        public Guid? Id { get; set; }

        public Guid? StationId { get; set; }

        public string? Code { get; set; } = null!;

        public bool? IsActive { get; set; }

        public bool? IsHeat { get; set; }

        public DateTime? CreateAt { get; set; }

        public DateTime? UpdateAt { get; set; }
    }

    public class ScanBoxResponse { }
}
