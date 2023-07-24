//using FINE.Service.DTO.Request;
//using FINE.Service.DTO.Request.Store;
//using FINE.Service.DTO.Response;
//using FINE.Service.Exceptions;
//using FINE.Service.Service;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;

//namespace FINE.API.Controllers.AdminControler
//{
//    [Route(Helpers.SettingVersionApi.ApiAdminVersion + "/store")]
//    [ApiController]
//    public class AdminStoreController : ControllerBase
//    {
//        private readonly IStoreService _storeService;

//        public AdminStoreController(IStoreService storeService)
//        {
//            _storeService = storeService;
//        }
//        /// <summary>
//        /// Get List Store
//        /// </summary>
//        [HttpGet]
//        [Authorize(Roles = "SystemAdmin, StoreManager")]
//        public async Task<ActionResult<BaseResponsePagingViewModel<StoreResponse>>> GetStores
//            ([FromQuery] StoreResponse request, [FromQuery] PagingRequest paging)
//        {
//            try
//            {
//                return await _storeService.GetStores(request, paging);
//            }
//            catch(ErrorResponse ex)
//            {
//                return BadRequest(ex.Error);
//            }
//        }

//        /// <summary>
//        /// Get Store By Id
//        /// </summary>
//        [HttpGet("{storeId}")]
//        [Authorize(Roles = "SystemAdmin, StoreManager")]
//        public async Task<ActionResult<BaseResponseViewModel<StoreResponse>>> GetStoreById
//            ([FromRoute] int storeId)
//        {
//            try
//            {
//                return await _storeService.GetStoreById(storeId);
//            }
//            catch(ErrorResponse ex)
//            {
//                return BadRequest(ex.Error);
//            }
//        }

//        /// <summary>
//        /// Create Store
//        /// </summary>
//        [HttpPost]
//        [Authorize(Roles = "SystemAdmin, StoreManager")]
//        public async Task<ActionResult<BaseResponseViewModel<StoreResponse>>> CreateStore
//            ([FromBody] CreateStoreRequest request)
//        {
//            try
//            {
//                return await _storeService.CreateStore(request);
//            }
//            catch(ErrorResponse ex)
//            {
//                return BadRequest(ex.Error); 
//            }
//        }

//        /// <summary>
//        /// Update Store
//        /// </summary>
//        [HttpPut("{storeId}")]
//        [Authorize(Roles = "SystemAdmin, StoreManager")]
//        public async Task<ActionResult<BaseResponseViewModel<StoreResponse>>> UpdateStore
//            ([FromRoute] int storeId, [FromBody] UpdateStoreRequest request)
//        {
//            try
//            {
//                return await _storeService.UpdateStore(storeId, request);
//            }
//            catch(ErrorResponse ex)
//            {
//                return BadRequest(ex.Error);
//            }
//        }
//    }
//}
