using FINE.Data.Entity;
using FINE.Service.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace FINE.Service.DTO.Response
{
    public class ProductCollectionResponse
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? BannerUrl { get; set; }
        public int StoreId { get; set; }
        public int Type { get; set; }
        public int? Position { get; set; }
        public bool Active { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; } 

        public virtual ICollection<ProductionCollectionItemResponse>? ProductionCollectionItems { get; set; }
    }
}
