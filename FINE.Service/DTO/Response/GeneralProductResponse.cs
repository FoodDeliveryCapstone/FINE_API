using FINE.Service.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class GeneralProductResponse
    {
        [Int] public int Id { get; set; }
        [Int] public int? GeneralProductId { get; set; }
        [String] public string? ProductCode { get; set; }
        [String] public string? ProductName { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int StoreId { get; set; }
        public string? StoreName { get; set; }
        public double BasePrice { get; set; }
        public double? SizePrice { get; set; }
        public string? Size { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }

        public ICollection<ProductResponse>? InverseGeneralProduct { get; set; }
    }
}
