using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Campus;
using FINE.Service.DTO.Request.Store_Category;
using FINE.Service.DTO.Response;
using FINE.Service.Service;
using Microsoft.AspNetCore.Mvc;


namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiVersion)]
    [ApiController]
    public class StoreCategoryController : ControllerBase
    {
        private readonly IStoreCategoryService _storeCategoryService;

        public StoreCategoryController(IStoreCategoryService storeCategoryService)
        {
            _storeCategoryService = storeCategoryService;
        }

        /// <summary>
        /// Get All Store Category
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<BaseResponsePagingViewModel<StoreCategoryResponse>>> GetAllStoreCategory([FromQuery] StoreCategoryResponse request, [FromQuery] PagingRequest paging)
        {
            return await _storeCategoryService.GetAllStoreCategory(request, paging);
        }

        /// <summary>
        /// Get Store Category By Id
        /// </summary>
        [HttpGet("{storeCategoryId}")]
        public async Task<ActionResult<BaseResponseViewModel<StoreCategoryResponse>>> GetStoreCategoryById([FromRoute] int storeCategoryId)
        {
            return await _storeCategoryService.GetStoreCategoryById(storeCategoryId);
        }

        /// <summary>
        /// Get List Store Category By StoreId
        /// </summary>
        [HttpGet("store/{storeId}")]
        public async Task<ActionResult<BaseResponsePagingViewModel<StoreCategoryResponse>>> GetStoreCategoryByStoreId([FromRoute] int storeId, [FromQuery] PagingRequest paging)
        {
            return await _storeCategoryService.GetStoreCategoryByStore(storeId, paging);
        }

        /// <summary>
        /// Create Store Category
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<BaseResponseViewModel<StoreCategoryResponse>>> CreateStoreCategory([FromBody] CreateStoreCategoryRequest request)
        {
            return await _storeCategoryService.CreateStoreCategory(request);
        }


        /// <summary>
        /// Update Store Category
        /// </summary>
        [HttpPut("{storeCategoryId}")]
        public async Task<ActionResult<BaseResponseViewModel<StoreCategoryResponse>>> UpdateCampus([FromRoute] int storeCategoryId, [FromBody] UpdateStoreCategoryRequest request)
        {
            return await _storeCategoryService.UpdateStoreCategory(storeCategoryId, request);
        }
    }
}
