using FINE.Service.DTO.Request;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Mvc;
using static FINE.Service.Helpers.Enum;

namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiVersion)]
    [ApiController]
    public class StationController : Controller
    {
        private readonly IStationService _stationService;

        public StationController(IStationService stationService)
        {
            _stationService = stationService;
        }

        /// <summary>
        /// lấy list station cho order
        /// </summary>
        [HttpGet("order")]
        public async Task<ActionResult<BaseResponsePagingViewModel<List<StationResponse>>>> GetStationByDestination(string destinationId, int numberBox)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);
                if (customerId == null)
                {
                    return Unauthorized();
                }
                var result = await _stationService.GetStationByDestinationForOrder(destinationId, numberBox);
                return Ok(result);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// lấy list station trong system
        /// </summary>
        [HttpGet("destination")]
        public async Task<ActionResult<BaseResponsePagingViewModel<StationResponse>>> GetStationByDestination(string destinationId, [FromQuery] PagingRequest paging)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);
                if (customerId == null)
                {
                    return Unauthorized();
                }
                var result = await _stationService.GetStationByDestination(destinationId, paging);
                return Ok(result);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// lock box cho order
        /// </summary>
        [HttpPut("orderBox")]
        public async Task<ActionResult<BaseResponseViewModel<int>>> LockBox(string stationId, string orderCode ,int numberBox)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);
                if (customerId == null)
                {
                    return Unauthorized();
                }
                var result = await _stationService.LockBox(stationId, orderCode ,numberBox);
                return Ok(result);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// update lock box cho order
        /// </summary>
        [HttpPut("orderBox")]
        public async Task<ActionResult<BaseResponseViewModel<int>>> UpdateLockBox(LockBoxUpdateTypeEnum type, string orderCode, string stationId)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);
                if (customerId == null)
                {
                    return Unauthorized();
                }
                var result = await _stationService.UpdateLockBox(type, orderCode, stationId);
                return Ok(result);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }
    }
}

