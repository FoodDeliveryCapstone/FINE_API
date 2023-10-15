using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Box;
using FINE.Service.DTO.Request.Destination;
using FINE.Service.DTO.Request.Station;
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
        /// Get All Box by Station Id
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager, Shipper")]
        [HttpGet("station/{stationId}")]
        public async Task<ActionResult<BaseResponsePagingViewModel<BoxResponse>>> GetAllBoxByStation([FromRoute] string stationId, [FromQuery] BoxResponse filter, [FromQuery] PagingRequest paging)
        {
            try
            {
                return await _boxService.GetBoxByStation(stationId, filter, paging);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Create Box
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        public async Task<ActionResult<BaseResponseViewModel<BoxResponse>>> CreateBox
            ([FromBody] CreateBoxRequest request)
        {
            try
            {
                return await _boxService.CreateBox(request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Update Box
        /// </summary>
        [HttpPut("{boxId}")]
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        public async Task<ActionResult<BaseResponseViewModel<BoxResponse>>> UpdateBox
            ([FromRoute] string boxId, [FromBody] UpdateBoxRequest request)
        {
            try
            {
                return await _boxService.UpdateBox(boxId, request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Get available box in station and timeslot
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        [HttpGet("availableBoxes")]
        public async Task<ActionResult<BaseResponsePagingViewModel<AvailableBoxResponse>>> GetAvailableBoxInStation([FromQuery] string stationId, string timeslotId)
        {
            try
            {
                return await _boxService.GetAvailableBoxInStation(stationId, timeslotId);
            }
            catch(ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }
    }
}
