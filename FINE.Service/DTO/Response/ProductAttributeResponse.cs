using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class ProductAttributeResponse
    {
        public Guid Id { get; set; }

        public Guid ProductId { get; set; }

        public string Name { get; set; } = null!;

        public string Code { get; set; } = null!;

        public string? Size { get; set; }

        public double Price { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreateAt { get; set; }

        public DateTime? UpdateAt { get; set; }
    }
}
