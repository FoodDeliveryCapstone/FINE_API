using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Box;
using FINE.Service.DTO.Request.Destination;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiAdminVersion + "/box")]
    [ApiController]
    public class AdminBoxController : ControllerBase
    {
        private readonly IBoxService _boxService;

        public AdminBoxController(IBoxService boxService)
        {
            _boxService = boxService;
        }

        /// <summary>
        /// Add Order to Box
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager, Shipper")]
        [HttpPost]
        public async Task<ActionResult<BaseResponseViewModel<OrderByStoreResponse>>> AddOrderToBox([FromBody] SystemAddOrderToBoxRequest request)
        {
            try
            {
                return await _boxService.SystemAddOrderToBox(request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Get All Box by Station Id
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager, Shipper")]
        [HttpGet("station/{stationId}")]
        public async Task<ActionResult<BaseResponsePagingViewModel<BoxResponse>>> GetAllBoxByStation([FromRoute] string stationId, [FromQuery] BoxResponse filter, [FromQuery] PagingRequest paging)
        {
            return await _boxService.GetBoxByStation(stationId, filter, paging);
        }
    }
}
