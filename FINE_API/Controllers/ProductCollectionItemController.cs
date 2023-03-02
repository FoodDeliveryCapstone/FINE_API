using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Campus;
using FINE.Service.DTO.Request.Product_Collection_Item;
using FINE.Service.DTO.Response;
using FINE.Service.Service;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiVersion)]
    [ApiController]
    public class ProductCollectionItemController : ControllerBase
    {
        private readonly IProductCollectionItemService _productCollectionItemService;
        public ProductCollectionItemController(IProductCollectionItemService productCollectionItemService)
        {
            _productCollectionItemService = productCollectionItemService;
        }

        /// <summary>
        /// Get All Product Collection Item
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<BaseResponsePagingViewModel<ProductCollectionItemResponse>>> GetAllProductCollectionItem([FromQuery] ProductCollectionItemResponse request, [FromQuery] PagingRequest paging)
        {
            return await _productCollectionItemService.GetAllProductCollectionItem(request, paging);
        }

        /// <summary>
        /// Get Product Collection Item By Id
        /// </summary>
        [HttpGet("{productCollectionItemId}")]
        public async Task<ActionResult<BaseResponseViewModel<ProductCollectionItemResponse>>> GetProductCollectionItemById([FromRoute] int productCollectionItemId)
        {
            return await _productCollectionItemService.GetProductCollectionItemById(productCollectionItemId);
        }

        /// <summary>
        /// Get All Product Collection Item By ProductId
        /// </summary>
        [HttpGet("product/{productId}")]
        public async Task<ActionResult<BaseResponsePagingViewModel<ProductCollectionItemResponse>>> GetProductCollectionItemByProduct([FromRoute] int productId, [FromQuery] PagingRequest paging)
        {
            return await _productCollectionItemService.GetProductCollectionItemByProduct(productId, paging);
        }

        /// <summary>
        /// Get All Product Collection Item By Product Collection Id
        /// </summary>
        [HttpGet("productCollection/{productCollectionId}")]
        public async Task<ActionResult<BaseResponsePagingViewModel<ProductCollectionItemResponse>>> GetProductCollectionItemByProductCollection([FromRoute] int productCollectionId, [FromQuery] PagingRequest paging)
        {
            return await _productCollectionItemService.GetProductCollectionItemByProductCollection(productCollectionId, paging);
        }

        /// <summary>
        /// Create Product Collection Item
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<BaseResponseViewModel<ProductCollectionItemResponse>>> CreateProductCollectionItem([FromBody] CreateProductCollectionItemRequest request)
        {
            return await _productCollectionItemService.CreateProductCollectionItem(request);
        }


        /// <summary>
        /// Update Product Collection Item
        /// </summary>
        [HttpPut("{productCollectionItemId}")]
        public async Task<ActionResult<BaseResponseViewModel<ProductCollectionItemResponse>>> UpdateCampus([FromRoute] int productCollectionItemId, [FromBody] UpdateProductCollectionItemRequest request)
        {
            return await _productCollectionItemService.UpdateProductCollectionItem(productCollectionItemId, request);
        }
    }
}
