using FINE.Service.DTO.Request.TimeSlot;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Response;
using Microsoft.AspNetCore.Mvc;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using FINE.Service.Exceptions;
using FINE.Service.Caches;

namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiVersion)]
    [ApiController]
    public class TimeslotController : Controller
    {
        private readonly ITimeslotService _timeslotService;

        public TimeslotController(ITimeslotService timeslotService)
        {
            _timeslotService = timeslotService;
        }

        /// <summary>
        /// Get Timeslot by DestinationId  
        /// </summary>
        [Cache(18000)]
        [HttpGet("destination/{destinationId}")]
        public async Task<ActionResult<BaseResponsePagingViewModel<TimeslotResponse>>> GetTimeslotsByDestination
            ([FromRoute] string destinationId, [FromQuery] PagingRequest paging)
        {
            try
            {
                var rs = await _timeslotService.GetTimeslotsByDestination(destinationId, paging);
                if (rs == null)
                {
                    return NotFound();
                }
                return Ok(rs);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Get list product in timeslot  
        /// </summary>
        [HttpGet("listProduct")]
        public async Task<ActionResult<BaseResponseViewModel<List<ProductResponse>>>> GetProductsInTimeSlot(string timeSlotId)
        {
            try
            {
                var rs = await _timeslotService.GetProductsInTimeSlot(timeSlotId);
                if (rs == null)
                {
                    return NotFound();
                }
                return Ok(rs);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }
    }
}
