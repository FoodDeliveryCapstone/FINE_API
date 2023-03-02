using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Request.BlogPost
{
    public class UpdateBlogPostRequest
    {
        public int StoreId { get; set; }
        public string Title { get; set; } = null!;
        public string BlogContent { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public bool Active { get; set; }
        public bool? IsDialog { get; set; }
        public string? Metadata { get; set; }
    }
}
