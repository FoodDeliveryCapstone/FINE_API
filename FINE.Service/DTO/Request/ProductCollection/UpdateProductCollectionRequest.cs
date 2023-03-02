using FINE.Service.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace FINE.Service.DTO.Request.ProductCollection
{
    public class UpdateProductCollectionRequest
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? BannerUrl { get; set; }
        public int StoreId { get; set; }
        public int Type { get; set; }
        public int? Position { get; set; }
        public bool Active { get; set; }

    }
}
