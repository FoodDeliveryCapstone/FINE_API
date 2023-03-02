using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.SystemCategory;
using FINE.Service.DTO.Response;
using FINE.Service.Service;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiVersion)]
    [ApiController]
    public class SystemCategoryController : ControllerBase
    {
        private readonly ISystemCategoryService _systemCategoryService;
        public SystemCategoryController(ISystemCategoryService systemCategoryService)
        {
            _systemCategoryService = systemCategoryService;
        }

        /// <summary>
        /// Get All System Category
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<BaseResponsePagingViewModel<SystemCategoryResponse>>> GetAllSystemCategory([FromQuery] SystemCategoryResponse request, [FromQuery] PagingRequest paging)
        {
            return await _systemCategoryService.GetAllSystemCategory(request, paging);
        }

        /// <summary>
        /// Get System Category By Id
        /// </summary>
        [HttpGet("{systemCategoryId}")]
        public async Task<ActionResult<BaseResponseViewModel<SystemCategoryResponse>>> GetSystemCategoryById([FromRoute] int systemCategoryId)
        {
            return await _systemCategoryService.GetSystemCategoryById(systemCategoryId);
        }

        /// <summary>
        /// Get System Category By Code
        /// </summary>
        [HttpGet("code/{systemCategoryCode}")]
        public async Task<ActionResult<BaseResponseViewModel<SystemCategoryResponse>>> GetSystemCategoryByCode([FromRoute] string systemCategoryCode)
        {
            return await _systemCategoryService.GetSystemCategoryByCode(systemCategoryCode);
        }

        /// <summary>
        /// Create System Category
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<BaseResponseViewModel<SystemCategoryResponse>>> CreateSystemCategory([FromBody] CreateSystemCategoryRequest request)
        {
            return await _systemCategoryService.CreateSystemCategory(request);
        }


        /// <summary>
        /// Update System Category
        /// </summary>
        [HttpPut("{systemCategoryId}")]
        public async Task<ActionResult<BaseResponseViewModel<SystemCategoryResponse>>> UpdateSystemCategory([FromRoute] int systemCategoryId, [FromBody] UpdateSystemCategoryRequest request)
        {
            return await _systemCategoryService.UpdateSystemCategory(systemCategoryId, request);
        }
    }
}
