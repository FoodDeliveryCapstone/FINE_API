using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class FloorResponse
    {
        public int? Id { get; set; }

        public int Number { get; set; }

        public int? CampusId { get; set; }

        public bool? IsActive { get; set; }

        public DateTime? CreateAt { get; set; }

        public DateTime? UpdateAt { get; set; }
    }
}
