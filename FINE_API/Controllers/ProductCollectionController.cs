using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.ProductCollection;
using FINE.Service.DTO.Response;
using FINE.Service.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiVersion)]
    [ApiController]
    public class ProductCollectionController : ControllerBase
    {
        private readonly IProductCollectionService _productCollectionService;

        public ProductCollectionController(IProductCollectionService productCollectionService)
        {
            _productCollectionService = productCollectionService;
        }

        /// <summary>
        /// Get List Product Collection
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<BaseResponsePagingViewModel<ProductCollectionResponse>>> GetAllProductCollection([FromQuery] ProductCollectionResponse request, [FromQuery] PagingRequest paging)
        {
            return await _productCollectionService.GetAllProductCollection(request, paging);
        }

        /// <summary>
        /// Get Product Collection By Id
        /// </summary>
        [HttpGet("{productCollectionId}")]
        public async Task<ActionResult<BaseResponseViewModel<ProductCollectionResponse>>> GetProductCollectionById([FromRoute] int productCollectionId)
        {
            return await _productCollectionService.GetProductCollectionById(productCollectionId);
        }

        /// <summary>
        /// Get List Product Collection By StoreId
        /// </summary>
        [HttpGet("store/{storeId}")]
        public async Task<ActionResult<BaseResponsePagingViewModel<ProductCollectionResponse>>> GetProductsByStoreId([FromRoute] int storeId, [FromQuery] PagingRequest paging)
        {
            return await _productCollectionService.GetProductCollectionByStore(storeId, paging);
        }

      
    }
}

