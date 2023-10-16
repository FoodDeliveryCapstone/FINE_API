using FINE.Service.DTO.Request;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Mvc;

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

        ///// <summary>
        ///// lấy thông tin khách hàng bằng token
        ///// </summary>
        //[HttpGet("destination/{destinationId}")]
        //public async Task<ActionResult<BaseResponsePagingViewModel<List<StationResponse>>>> GetStationByDestination(string destinationId, string orderCode)
        //{
        //    try
        //    {
        //        var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        //        var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);
        //        if (customerId == null)
        //        {
        //            return Unauthorized();
        //        }
        //        var result = await _stationService.GetStationByDestinationForOrder(destinationId, orderCode);
        //        return Ok(result);
        //    }
        //    catch (ErrorResponse ex)
        //    {
        //        return BadRequest(ex.Error);
        //    }
        //}

        ///// <summary>
        ///// lấy thông tin khách hàng bằng token
        ///// </summary>
        [HttpGet("destination/{destinationId}")]
        public async Task<ActionResult<BaseResponsePagingViewModel<StationResponse>>> GetStationByDestination(string destinationId, [FromQuery] PagingRequest paging)
        {
            try
            {
                //var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                //var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);
                //if (customerId == null)
                //{
                //    return Unauthorized();
                //}
                var result = await _stationService.GetStationByDestination(destinationId, paging);
                return Ok(result);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }
    }
}

