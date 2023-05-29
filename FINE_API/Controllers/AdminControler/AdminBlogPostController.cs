using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.BlogPost;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers.AdminControler;

[Route(Helpers.SettingVersionApi.ApiAdminVersion + "/blogPost")]
[ApiController]
public class AdminBlogPostController : ControllerBase
{
    private readonly IBlogPostService _blogPostService;
    public AdminBlogPostController(IBlogPostService blogPostService)
    {
        _blogPostService = blogPostService;
    }

    /// <summary>
    /// Get List Blog Posts    
    /// </summary>
    /// 
    [Authorize(Roles = "SystemAdmin, StoreManager")]
    [HttpGet]
    public async Task<ActionResult<BaseResponsePagingViewModel<BlogPostResponse>>> GetBlogPost
        ([FromQuery] BlogPostResponse filter, [FromQuery] PagingRequest paging)
    {
        try
        {
            return await _blogPostService.GetBlogPost(filter, paging);
        }
        catch (ErrorResponse ex)
        {
            return BadRequest(ex.Error);
        }
    }

    /// <summary>
    /// Get List Blog Posts By Id     
    /// </summary>
    /// 
    [Authorize(Roles = "SystemAdmin, StoreManager")]
    [HttpGet("{blogPostId}")]

    public async Task<ActionResult<BaseResponseViewModel<BlogPostResponse>>> GetBlogPostById
        ([FromRoute] int blogPostId)
    {
        try
        {
            return await _blogPostService.GetBlogPostById(blogPostId);
        }
        catch (ErrorResponse ex)
        {
            return BadRequest(ex.Error);
        }

    }

    /// <summary>
    /// Create Blog Post
    /// </summary>
    ///
    [Authorize(Roles = "SystemAdmin, StoreManager")]
    [HttpPost]
    public async Task<ActionResult<BaseResponseViewModel<BlogPostResponse>>> CreateBlogPost
        ([FromBody] CreateBlogPostRequest request)
    {
        try
        {
            return await _blogPostService.CreateBlogPost(request);
        }
        catch (ErrorResponse ex)
        {
            return BadRequest(ex.Error);
        }
    }

    /// <summary>
    /// Update Blog Posts    
    /// </summary>
    /// 
    [Authorize(Roles = "SystemAdmin, StoreManager")]
    [HttpPut("{blogPostId}")]
    public async Task<ActionResult<BaseResponseViewModel<BlogPostResponse>>> UpdateBlogPost
        ([FromRoute] int blogPostId, [FromBody] UpdateBlogPostRequest request)
    {
        try
        {
            return await _blogPostService.UpdateBlogPost(blogPostId, request);
        }
        catch (ErrorResponse ex)
        {
            return BadRequest(ex.Error);
        }
    }

}
