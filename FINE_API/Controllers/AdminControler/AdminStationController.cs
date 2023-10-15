using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Station;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiAdminVersion + "/station")]
    [ApiController]
    public class AdminStationController : ControllerBase
    {
        private readonly IStationService _stationService;

        public AdminStationController(IStationService stationService)
        {
            _stationService = stationService;
        }

        /// <summary>
        /// Get Station by Id
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager, Shipper")]
        [HttpGet("{stationId}")]
        public async Task<ActionResult<BaseResponseViewModel<StationResponse>>> GetStationById([FromRoute] string stationId)
        {
            try
            {
                var result = await _stationService.GetStationById(stationId);
                return Ok(result);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Get Stations
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager, Shipper")]
        [HttpGet("destination/{destinationId}")]
        public async Task<ActionResult<BaseResponsePagingViewModel<StationResponse>>> GetStationByDestination(string destinationId, [FromQuery] PagingRequest paging)
        {
            try
            {
                var result = await _stationService.GetStationByDestination(destinationId, paging);
                return Ok(result);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Create Station
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        public async Task<ActionResult<BaseResponseViewModel<StationResponse>>> CreateStation
            ([FromBody] CreateStationRequest request)
        {
            try
            {
                return await _stationService.CreateStation(request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Update Station
        /// </summary>
        [HttpPut("{stationId}")]
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        public async Task<ActionResult<BaseResponseViewModel<StationResponse>>> UpdateStation
            ([FromRoute] string stationId, [FromBody] UpdateStationRequest request)
        {
            try
            {
                return await _stationService.UpdateStation(stationId, request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

    }
}
