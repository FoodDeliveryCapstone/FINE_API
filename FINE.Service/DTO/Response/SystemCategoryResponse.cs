using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class SystemCategoryResponse
    {
        public int? Id { get; set; }
        public string? CategoryCode { get; set; }

        public string? CategoryName { get; set; }

        public string? ImageUrl { get; set; }

        public bool? ShowOnHome { get; set; }

        public DateTime? CreateAt { get; set; }

        public DateTime? UpdateAt { get; set; }
    }
}
