using FINE.Service.DTO.Request.TimeSlot;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Response;
using Microsoft.AspNetCore.Mvc;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using FINE.Service.Exceptions;

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

        ///// <summary>
        ///// Get List Timeslot    
        ///// </summary>

        //[HttpGet]
        //public async Task<ActionResult<BaseResponsePagingViewModel<TimeslotResponse>>> GetTimeslots
        //    ([FromQuery] TimeslotResponse filter, [FromQuery] PagingRequest paging)
        //{
        //    try
        //    {
        //        return await _timeslotService.GetTimeSlots(filter, paging);
        //    }
        //    catch (ErrorResponse ex)
        //    {
        //        return BadRequest(ex.Error);
        //    }
        //}

        ///// <summary>
        ///// Get Timeslot by Id  
        ///// </summary>

        //[HttpGet("{timeslotId}")]
        //public async Task<ActionResult<BaseResponseViewModel<TimeslotResponse>>> GetTimeslotById
        //    ([FromRoute] int timeslotId)
        //{
        //    try
        //    {
        //        return await _timeslotService.GetTimeSlotById(timeslotId);
        //    }
        //    catch (ErrorResponse ex)
        //    {
        //        return BadRequest(ex.Error);
        //    }
        //}

        /// <summary>
        /// Get Timeslot by DestinationId  
        /// </summary>

        [HttpGet("destination/{destinationId}")]
        public async Task<ActionResult<BaseResponsePagingViewModel<TimeslotResponse>>> GetTimeslotsByDestination
            ([FromRoute] string destinationId, [FromQuery] PagingRequest paging)
        {
            try
            {
                return await _timeslotService.GetTimeslotsByDestination(destinationId, paging);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

    }
}
