using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.BlogPost;
using FINE.Service.DTO.Response;
using FINE.Service.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiVersion)]
    [ApiController]
    public class BlogPostController : ControllerBase
    {
        private readonly IBlogPostService _blogPostService;

        public BlogPostController(IBlogPostService blogPostService)
        {
            _blogPostService = blogPostService;
        }

        /// <summary>
        /// Get List Blog Posts    
        /// </summary>
        /// 
        [HttpGet]
        public async Task<ActionResult<BaseResponsePagingViewModel<BlogPostResponse>>> GetBlogPost
            ([FromQuery] BlogPostResponse filter, [FromQuery] PagingRequest paging)
        {
            return await _blogPostService.GetBlogPost(filter, paging);
        }

        /// <summary>
        /// Get List Blog Posts By Id     
        /// </summary>
        /// 
        [HttpGet("{blogPostId}")]

        public async Task<ActionResult<BaseResponseViewModel<BlogPostResponse>>> GetBlogPostById
            ([FromRoute] int blogPostId)
        {
            return await _blogPostService.GetBlogPostById(blogPostId);
        }

        /// <summary>
        /// Create Blog Post
        /// </summary>
        /// 
        [HttpPost]
        public async Task<ActionResult<BaseResponseViewModel<BlogPostResponse>>> CreateBlogPost
            ([FromBody] CreateBlogPostRequest request)
        {
            return await _blogPostService.CreateBlogPost(request);
        }

        /// <summary>
        /// Update Blog Posts    
        /// </summary>
        /// 
        [HttpPut("{blogPostId}")]
        public async Task<ActionResult<BaseResponseViewModel<BlogPostResponse>>> UpdateBlogPost
           ([FromRoute] int blogPostId, [FromBody] UpdateBlogPostRequest request)
        {
            return await _blogPostService.UpdateBlogPost(blogPostId, request);
        }

    }
}
