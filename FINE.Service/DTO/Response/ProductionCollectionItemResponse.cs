using FINE.Data.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class ProductionCollectionItemResponse
    {
        public int Id { get; set; }

        public int ProductCollectionId { get; set; }

        public int ProductId { get; set; }

        public bool Active { get; set; }

        public DateTime CreateAt { get; set; }

        public DateTime? UpdateAt { get; set; }

        public GeneralProductResponse? Product { get; set; }
    }
}
