using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Product;
using FINE.Service.DTO.Request.ProductAttribute;
using FINE.Service.DTO.Request.ProductInMenu;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers.AdminControler
{
    [Route(Helpers.SettingVersionApi.ApiAdminVersion + "/productInMenu")]
    [ApiController]
    public class AdminProductInMenuController : Controller
    {
        private readonly IProductInMenuService _productInMenuService;

        public AdminProductInMenuController(IProductInMenuService productInMenuService)
        {
            _productInMenuService = productInMenuService;
        }

        /// <summary>
        /// Get List Product in Menu
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        [HttpGet("{menuId}")]
        public async Task<ActionResult<BaseResponsePagingViewModel<ProductInMenuResponse>>> GetProductInMenu(string menuId, [FromQuery]PagingRequest paging)
        {
            try
            {
                return await _productInMenuService.GetProductInMenu(menuId, paging);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Update Product in menu
        /// </summary>    
        [Authorize(Roles = "SystemAdmin")]
        [HttpPut]
        public async Task<ActionResult<BaseResponseViewModel<dynamic>>> UpdateProductInMenu(string productInMenuId, [FromBody] UpdateProductInMenuRequest request)
        {
            try
            {
                return await _productInMenuService.UpdateProductInMenu(productInMenuId, request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Add Product to menu
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<ActionResult<BaseResponseViewModel<dynamic>>> AddProductToMenu([FromBody] AddProductToMenuRequest request)
        {
            try
            {
                return await _productInMenuService.AddProductIntoMenu(request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }
    }
}
