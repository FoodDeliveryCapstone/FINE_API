using FINE.Service.DTO.Request.Product;
using FINE.Service.DTO.Request.ProductAttribute;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers.AdminControler
{
    [Route(Helpers.SettingVersionApi.ApiAdminVersion + "/productAttribute")]
    [ApiController]
    public class AdminProductAttributeController : Controller
    {
        private readonly IProductAttributeService _productAttributeService;
        public AdminProductAttributeController(IProductAttributeService productAttributeService)
        {
            _productAttributeService = productAttributeService;
        }

        /// <summary>
        /// Update Product attribute
        /// </summary>    
        [Authorize(Roles = "SystemAdmin")]
        [HttpPut]
        public async Task<ActionResult<BaseResponseViewModel<ProductResponse>>> UpdateProductAttribute(string productAttributeId, [FromBody] UpdateProductAttributeRequest request)
        {
            try
            {
                return await _productAttributeService.UpdateProductAttribute(productAttributeId, request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Create Product attribute
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<ActionResult<BaseResponseViewModel<ProductResponse>>> CreateProduct(string productId, [FromBody] List<CreateProductAttributeRequest> request)
        {
            try
            {
                return await _productAttributeService.CreateProductAttribute(productId, request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }
    }
}
