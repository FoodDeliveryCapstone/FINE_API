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
        public string? Code { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsHeat { get; set; }
        public DateTime? CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }
    }

    public class AvailableBoxResponse
    {
        public Guid? Id { get; set; }
        public string? Code { get; set; }
        public int Status { get; set; }
        public bool? IsHeat { get; set; }
        public Guid StationId { get; set; }
    }

    public class AvailableBoxInStationResponse
    {
        public Guid StationId { get; set; }
        public string? StationName { get; set; }
        public List<AvailableBoxResponse> ListBox { get; set; }
    }
}
