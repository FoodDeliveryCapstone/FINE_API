using FINE.Service.DTO.Request;

using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;

using Microsoft.AspNetCore.Mvc;
namespace FINE.API.Controllers;

[Route(Helpers.SettingVersionApi.ApiVersion)]
[ApiController]
public class ProductInMenuController : ControllerBase
{

    private readonly IProductInMenuService _productInMenuService;
    public ProductInMenuController(IProductInMenuService productInMenuService)
    {
        
        _productInMenuService= productInMenuService;
    }

    /// <summary>
    /// Get Product By ProductInMenuId
    /// </summary>
    [HttpGet("{productInMenuId}")]
    public async Task<ActionResult<BaseResponseViewModel<ProductInMenuResponse>>> GetProductByProductInMenu([FromRoute] int productInMenuId)
    {
        try
        {
            return await _productInMenuService.GetProductByProductInMenu(productInMenuId);
        }
        catch (ErrorResponse ex)
        {
            return BadRequest(ex.Error);
        }
    }

    /// <summary>
    /// Get List Product in Menu By StoreId
    /// </summary>

    [HttpGet("productInMenu/store/{storeId}")]
    public async Task<ActionResult<List<ProductInMenuResponse>>> GetProductInMenuByStore([FromRoute] int storeId, [FromQuery] PagingRequest paging)
    {
        try
        {
            return await _productInMenuService.GetProductInMenuByStore(storeId, paging);
        }
        catch (ErrorResponse ex)
        {
            return BadRequest(ex.Error);
        }
    }

    /// <summary>
    /// Get Product In Menu Best Seller
    /// </summary>
    ///
    [HttpGet("ProductBestSeller")]
    public async Task<ActionResult<BaseResponsePagingViewModel<ProductInMenuResponse>>> GetProductBestSeller([FromQuery] PagingRequest paging)
    {
        try
        {
            return await _productInMenuService.GetProductInMenuBestSeller(paging);
        }
        catch (ErrorResponse ex)
        {
            return BadRequest(ex.Error);
        }
    }

}
