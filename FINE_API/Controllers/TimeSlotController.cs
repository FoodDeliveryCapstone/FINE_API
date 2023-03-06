using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.TimeSlot;
using FINE.Service.DTO.Response;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiVersion)]
    [ApiController]
    public class TimeSlotController : ControllerBase
    {
        private readonly ITimeSlotService _timeSlotService;
        public TimeSlotController(ITimeSlotService timeSlotService)
        {
            _timeSlotService = timeSlotService;
        }

        /// <summary>
        /// Get List Product by StoreId and TimeslotId
        /// </summary>
        [HttpGet("{timeslotId}/store/{storeId}/product")]
        public async Task<ActionResult<BaseResponsePagingViewModel<TimeSlotResponse>>> GetProductsByStoreAndTimeslot([FromRoute] int storeId, [FromRoute] int timeslotId, [FromQuery] PagingRequest paging)
        {
            return await _timeSlotService.GetProductByStoreAndTimeslot(storeId, timeslotId, paging);
        }

        /// <summary>
        /// Get List Product by TimeslotId
        /// </summary>
        [HttpGet("{timeslotId}/product")]
        public async Task<ActionResult<BaseResponsePagingViewModel<TimeSlotResponse>>> GetProductsByTimeslot([FromRoute] int timeslotId, [FromQuery] PagingRequest paging)
        {
            return await _timeSlotService.GetProductByTimeslot(timeslotId, paging);
        }
    }
}
