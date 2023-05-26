using FINE.Data.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class MenuResponse
    {
        public int? Id { get; set; }

        public int? TimeSlotId { get; set; }

        public string? MenuName { get; set; }
        public string? ImgUrl { get; set; }

        public bool? IsActive { get; set; }

        public DateTime? CreateAt { get; set; }

        public DateTime? UpdateAt { get; set; }

       public ICollection<ProductInMenuResponse>? Products { get; set;}
    }
}
