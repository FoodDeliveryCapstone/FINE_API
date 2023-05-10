using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Product;
using FINE.Service.DTO.Request.ProductInMenu;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace FINE.API.Controllers.AdminController;

[Route(Helpers.SettingVersionApi.ApiAdminVersion + "/product")]
[ApiController]
public class AdminProductController : Controller
{
    private readonly IProductService _productService;
    private readonly IAddProductToMenuService _addProductToMenuService;
    public AdminProductController(IProductService productService, IAddProductToMenuService addProductToMenuService)
    {
        _productService = productService;
        _addProductToMenuService = addProductToMenuService;
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
    /// Get List Product By MenuId
    /// </summary>
    [Authorize(Roles = "SystemAdmin, StoreManager")]
    [HttpGet("menu/{menuId}")]
    public async Task<ActionResult<BaseResponsePagingViewModel<ProductInMenuResponse>>> GetProductsByMenuId([FromRoute] int menuId, [FromQuery] PagingRequest paging)
    {
        return await _productService.GetProductByMenu(menuId, paging);
    }

    /// <summary>
    /// Create Product
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SystemAdmin, StoreManager")]
    public async Task<ActionResult<BaseResponseViewModel<ProductResponse>>> CreateProduct([FromBody] CreateProductRequest request)
    {
        try
        {
            return await _productService.CreateProduct(request);
        }
        catch (ErrorResponse ex)
        {
            return BadRequest(ex.Error);
        }
    }
    
    /// <summary>
    /// Update Product
    /// </summary>    
    [Authorize(Roles = "SystemAdmin, StoreManager")]
    [HttpPut("{productId}")]
    public async Task<ActionResult<BaseResponseViewModel<ProductResponse>>> UpdateProduct([FromRoute] int productId, [FromBody] UpdateProductRequest request)
    {
        try
        {
            return await _productService.UpdateProduct(productId, request);
        }
        catch(ErrorResponse ex)
        {
            return BadRequest(ex.Error);
        }
    }

    /// <summary>
    /// Add Product to Menu
    /// </summary>
    [HttpPost("productInMenu")]
    [Authorize(Roles = "SystemAdmin, StoreManager")]
    public async Task<ActionResult<BaseResponseViewModel<ProductInMenuResponse>>> AddProductToMenu([FromBody] AddProductToMenuRequest request)
    {
        try
        {
            return await _addProductToMenuService.AddProductIntoMenu(request);
        }
        catch (ErrorResponse ex)
        {
            return BadRequest(ex.Error);
        }
    }

    /// <summary>
    /// Update Product in Menu
    /// </summary>    
    [Authorize(Roles = "SystemAdmin, StoreManager")]
    [HttpPut("productInMenu/{productInMenuId}")]
    public async Task<ActionResult<BaseResponseViewModel<ProductInMenuResponse>>> UpdateProductInMenu([FromRoute] int productInMenuId, [FromBody] UpdateProductInMenuRequest request)
    {
        try
        {
            return await _addProductToMenuService.UpdateProductInMenu(productInMenuId, request);
        }
        catch (ErrorResponse ex)
        {
            return BadRequest(ex.Error);
        }
    }

    /// <summary>
    /// Update All Product in Menu Status
    /// </summary>    
    //[Authorize(Roles = "SystemAdmin, StoreManager")]
    [HttpPut("productInMenu/status")]
    public async Task<ActionResult<BaseResponseViewModel<AddProductToMenuResponse>>> UpdateAllProductInMenuStatus([FromBody] UpdateAllProductInMenuStatusRequest request)
    {
        try
        {
            return await _addProductToMenuService.UpdateAllProductInMenuStatus(request);
        }
        catch (ErrorResponse ex)
        {
            return BadRequest(ex.Error);
        }
    }
}