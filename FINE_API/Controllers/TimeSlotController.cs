using FINE.Service.DTO.Request;
using FINE.Service.DTO.Response;
using FINE.Service.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiVersion)]
    [ApiController]
    public class TimeSlotController : ControllerBase
    {
        private readonly ITimeSlotService _timeslotService;

        public TimeSlotController(ITimeSlotService timeslotService)
        {
            _timeslotService = timeslotService;
        }

        /// <summary>
        /// Get Product By Timeslot Id       
        /// </summary>
        /// 

        [HttpGet("{timeslotId}")]
        public async Task<ActionResult<BaseResponsePagingViewModel<TimeSlotResponse>>> GetTimeslots
            ([FromRoute] int timeslotId, [FromQuery] PagingRequest paging)
        {
            return await _timeslotService.GetProductByTimeSlot(timeslotId, paging);
        }

    }
}
