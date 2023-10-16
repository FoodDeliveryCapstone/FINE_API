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
}