using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Store;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiVersion)]
    [ApiController]
    public class StoreController : ControllerBase
    {
        private readonly IStoreService _storeService;

        public StoreController(IStoreService storeService)
        {
            _storeService = storeService;
        }

        /// <summary>
        /// Get List Store
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<BaseResponsePagingViewModel<StoreResponse>>> GetStores
            ([FromQuery] StoreResponse request, [FromQuery] PagingRequest paging)
        {
            return await _storeService.GetStores(request, paging);
        }

        /// <summary>
        /// Get Store By Id
        /// </summary>
        [HttpGet("{storeId}")]
        public async Task<ActionResult<BaseResponseViewModel<StoreResponse>>> GetStoreById
            ([FromRoute] int storeId)
        {
            return await _storeService.GetStoreById(storeId);
        }

        /// <summary>
        /// Get List Store By TimeslotId
        /// </summary>

        [HttpGet("/timeslot/{timeslotId}")]
        public async Task<ActionResult<BaseResponsePagingViewModel<StoreResponse>>> GetStoreByTimeslot([FromRoute] int timeslotId, [FromQuery] PagingRequest paging)
        {
            try
            {
                return await _storeService.GetStoreByTimeslot(timeslotId, paging);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Create Store
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<BaseResponseViewModel<StoreResponse>>> CreateStore
            ([FromBody] CreateStoreRequest request)
        {
            return await _storeService.CreateStore(request);
        }

        /// <summary>
        /// Update Store
        /// </summary>
        [HttpPut("{storeId}")]
        public async Task<ActionResult<BaseResponseViewModel<StoreResponse>>> UpdateStore
            ([FromRoute] int storeId, [FromBody] UpdateStoreRequest request)
        {
            return await _storeService.UpdateStore(storeId, request);
        }
    }
}
