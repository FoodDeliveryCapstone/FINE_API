using FINE.Service.Caches;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Product;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace FINE.API.Controllers;

[Route(Helpers.SettingVersionApi.ApiVersion)]
[ApiController]
public class ProductController : Controller
{
    private readonly IProductService _productService;
    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    ///// <summary>
    ///// Get List Product
    ///// </summary>
    //[HttpGet]
    //public async Task<ActionResult<BaseResponsePagingViewModel<ProductResponse>>> GetProducts([FromQuery] ProductResponse request, [FromQuery] PagingRequest paging)
    //{
    //    var rs = await _productService.GetProducts(request, paging);
    //    return Ok(rs);
    //}

    /// <summary>
    /// Get Product By Id
    /// </summary>

    [HttpGet("{id}")]
    public async Task<ActionResult<BaseResponseViewModel<ProductResponse>>> GetProductById([FromRoute] string id)
    {
        try
        {
            return await _productService.GetProductById(id);
        }
        catch (ErrorResponse ex)
        {
            throw ex;
        }
    }

    ///// <summary>
    ///// Get List Product By StoreId
    ///// </summary>

    //[HttpGet("store/{storeId}")]
    //public async Task<ActionResult<BaseResponsePagingViewModel<ProductResponse>>> GetProductsByStoreId([FromRoute] int storeId, [FromQuery] PagingRequest paging)
    //{
    //    return await _productService.GetProductByStore(storeId, paging);
    //}

    /////<summary>
    /////Get product By CategoryId
    ///// </summary>
    //[HttpGet("category/{categoryId}")]
    //public async Task<ActionResult<BaseResponsePagingViewModel<ProductResponse>>> GetProductByCategory(
    //    [FromRoute] int categoryId, [FromQuery] PagingRequest paging)
    //{
    //    return await _productService.GetProductByCategory(categoryId, paging);
    //}

    ///// <summary>
    ///// Get List Product By MenuId
    ///// </summary>

    //[HttpGet("menu/{menuId}")]
    //public async Task<ActionResult<BaseResponsePagingViewModel<ProductInMenuResponse>>> GetProductsByMenuId([FromRoute] int menuId, [FromQuery] PagingRequest paging)
    //{
    //    return await _productService.GetProductByMenu(menuId, paging);
    //}


}