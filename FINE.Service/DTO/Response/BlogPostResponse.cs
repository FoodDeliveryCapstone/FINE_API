using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class BlogPostResponse
    {
        public int Id { get; set; }
        public int StoreId { get; set; }
        public string? Title { get; set; }
        public string? BlogContent { get; set; }
        public string? ImageUrl { get; set; }
        public bool Active { get; set; }
        public bool? IsDialog { get; set; }
        public string? Metadata { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }
    }
}
