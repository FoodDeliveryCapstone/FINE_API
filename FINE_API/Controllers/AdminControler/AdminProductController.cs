using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Product;
using FINE.Service.DTO.Response;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace FINE.API.Controllers.AdminController;

[Route(Helpers.SettingVersionApi.ApiAdminVersion + "/product")]
[ApiController]
public class AdminProductController : Controller
{
    private readonly IProductService _productService;
    public AdminProductController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Get List Product
    /// </summary>
    [Authorize(Roles = "SystemAdmin")]
    [HttpGet]
    public async Task<ActionResult<BaseResponsePagingViewModel<ProductResponse>>> GetProducts([FromQuery] ProductResponse request, [FromQuery] PagingRequest paging)
    {
        return await _productService.GetProducts(request, paging);
    }

    /// <summary>
    /// Get Product By Id
    /// </summary>
    [Authorize(Roles = "SystemAdmin, StoreManager")]
    [HttpGet("{productId}")]
    public async Task<ActionResult<BaseResponseViewModel<ProductResponse>>> GetProductById([FromRoute] int productId)
    {
        return await _productService.GetProductById(productId);
    }

    /// <summary>
    /// Get List Product By StoreId
    /// </summary>
    [Authorize(Roles = "SystemAdmin, StoreManager")]
    [HttpGet("store/{storeId}")]
    public async Task<ActionResult<BaseResponsePagingViewModel<ProductResponse>>> GetProductsByStoreId([FromRoute] int storeId, [FromQuery] PagingRequest paging)
    {
        return await _productService.GetProductByStore(storeId, paging);
    }

    ///<summary>
    ///Get product By CategoryId
    /// </summary>
    [Authorize(Roles = "SystemAdmin, StoreManager")]
    [HttpGet("category/{categoryId}")]
    public async Task<ActionResult<BaseResponsePagingViewModel<ProductResponse>>> GetProductByCategory(
        [FromRoute] int categoryId, [FromQuery] PagingRequest paging)
    {
        return await _productService.GetProductByCategory(categoryId, paging);
    }

    /// <summary>
    /// Create Product
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SystemAdmin, StoreManager")]
    public async Task<ActionResult<BaseResponseViewModel<ProductResponse>>> CreateProduct([FromBody] CreateProductRequest request)
    {
        return await _productService.CreateProduct(request);
    }
    
    /// <summary>
    /// Update Product
    /// </summary>    
    [Authorize(Roles = "SystemAdmin, StoreManager")]
    [HttpPut("{productId}")]
    public async Task<ActionResult<BaseResponseViewModel<ProductResponse>>> UpdateProduct([FromRoute] int productId, [FromBody] UpdateProductRequest request)
    {
        return await _productService.UpdateProduct(productId, request);
    }
}