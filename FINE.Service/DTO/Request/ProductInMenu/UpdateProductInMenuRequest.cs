using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;

namespace FINE.Service.DTO.Request.ProductInMenu
{
    public class UpdateProductInMenuRequest
    {
        public ProductInMenuStatusEnum Status { get; set; }
        public bool IsActive { get; set; }
    }

    public class UpdateAllProductInMenuStatusRequest
    {

    }
}
