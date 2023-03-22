using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Request.ProductInMenu
{
    public class UpdateProductInMenuRequest
    {
        public double Price { get; set; }
        public int Status { get; set; }
        public bool? IsAvailable { get; set; }
    }
}
