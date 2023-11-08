using FINE.Service.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class ProductInMenuResponse
    {
        public Guid Id { get; set; }

        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }

        public Guid MenuId { get; set; }

        public bool IsActive { get; set; }

        public int Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        //public ProductAttributeResponse Product { get; set; }
    }
}
