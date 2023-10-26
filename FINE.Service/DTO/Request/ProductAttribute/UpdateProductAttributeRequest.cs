using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;

namespace FINE.Service.DTO.Request.ProductAttribute
{
    public class UpdateProductAttributeRequest
    {
        public string Name { get; set; } = null!;

        public string Code { get; set; } = null!;
        public string? Size { get; set; }

        public double Price { get; set; }
        public ProductRotationTypeEnum RotationType { get; set; }

        public double Height { get; set; }

        public double Width { get; set; }

        public double Length { get; set; }
        public bool IsActive { get; set; }
    }
}
