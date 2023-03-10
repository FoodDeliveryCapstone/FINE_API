using FINE.Service.DTO.Request;
using FINE.Service.DTO.Response;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace FINE.API.Controllers;

[Route(Helpers.SettingVersionApi.ApiVersion)]
[ApiController]
public class ProductInMenuController : ControllerBase
    {
    private readonly IProductService _productService;
    public ProductInMenuController(IProductService productService)
    {
        _productService = productService;
    }


    /// <summary>
    /// Get Product By ProductInMenuId
    /// </summary>

    [HttpGet("{productInMenuId}")]
    public async Task<ActionResult<BaseResponseViewModel<ProductInMenuResponse>>> GetProductByProductInMenu([FromRoute] int productInMenuId)
    {
        return await _productService.GetProductByProductInMenu(productInMenuId);
    }
}
