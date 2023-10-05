using FINE.Service.DTO.Request.TimeSlot;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Response;
using Microsoft.AspNetCore.Mvc;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using FINE.Service.Caches;
using FINE.Service.Exceptions;

namespace FINE.API.Controllers.AdminControler
{
    [Route(Helpers.SettingVersionApi.ApiAdminVersion + "/timeslot")]
    [ApiController]
    public class AdminTimeSlotController : Controller
    {
        private readonly ITimeslotService _timeslotService;

        public AdminTimeSlotController(ITimeslotService timeslotService)
        {
            _timeslotService = timeslotService;
        }
        /// <summary>
        /// Get Timeslot by DestinationId  
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager, Shipper")]
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

        ///// <summary>
        ///// Get List Timeslot    
        ///// </summary>
        //[Authorize(Roles = "SystemAdmin")]
        //[HttpGet]
        //public async Task<ActionResult<BaseResponsePagingViewModel<TimeslotResponse>>> GetTimeslots
        //    ([FromQuery] TimeslotResponse filter, [FromQuery] PagingRequest paging)
        //{
        //    return await _timeslotService.GetTimeSlots(filter, paging);
        //}

        ///// <summary>
        ///// Get Timeslot by Id  
        ///// </summary>
        //[Authorize(Roles = "SystemAdmin")]
        //[HttpGet("{timeslotId}")]
        //public async Task<ActionResult<BaseResponseViewModel<TimeslotResponse>>> GetTimeslotById
        //    ([FromRoute] int timeslotId)
        //{
        //    return await _timeslotService.GetTimeSlotById(timeslotId);
        //}

        /// <summary>
        /// Create Timeslot
        /// </summary>
        [Authorize(Roles = "SystemAdmin")]
        [HttpPost]
        public async Task<ActionResult<BaseResponseViewModel<TimeslotResponse>>> CreateTimeslot
            ([FromBody] CreateTimeslotRequest request)
        {
            return await _timeslotService.CreateTimeslot(request);
        }

        /// <summary>
        /// Update Timeslot
        /// </summary>
        [Authorize(Roles = "SystemAdmin")]
        [HttpPut("{timeslotId}")]
        public async Task<ActionResult<BaseResponseViewModel<TimeslotResponse>>> UpdateTimeslot
            ([FromRoute] string timeslotId, [FromBody] UpdateTimeslotRequest request)

        {
            return await _timeslotService.UpdateTimeslot(timeslotId, request);
        }
    }
}
