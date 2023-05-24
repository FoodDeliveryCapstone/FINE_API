﻿using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.BlogPost;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Mvc;
namespace FINE.API.Controllers;

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

}

