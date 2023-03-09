using FINE.Service.DTO.Request.TimeSlot;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Response;
using Microsoft.AspNetCore.Mvc;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;

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
        /// Get List Timeslot    
        /// </summary>
        
        [HttpGet]
        public async Task<ActionResult<BaseResponsePagingViewModel<TimeslotResponse>>> GetTimeslots
            ([FromQuery] TimeslotResponse filter, [FromQuery] PagingRequest paging)
        {
            return await _timeslotService.GetTimeSlots(filter, paging);
        }

        /// <summary>
        /// Get Timeslot by Id  
        /// </summary>
       
        [HttpGet("{timeslotId}")]
        public async Task<ActionResult<BaseResponseViewModel<TimeslotResponse>>> GetTimeslotById
            ([FromRoute] int timeslotId)
        {
            return await _timeslotService.GetTimeSlotById(timeslotId);
        }

    }
}
