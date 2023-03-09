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
    public class RoomController : ControllerBase
    {
        private readonly IRoomService _roomService;

        public RoomController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        /// <summary>
        /// Get List Room  
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<BaseResponsePagingViewModel<RoomResponse>>> GetRooms
            ([FromQuery] RoomResponse filter, [FromQuery] PagingRequest paging)
        {
            try
            {
                return await _roomService.GetRooms(filter, paging);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Get Room by FloorId and AreaId  
        /// </summary>

        [HttpGet("floor/{floorId}/area/{areaId}")]
        public async Task<ActionResult<BaseResponsePagingViewModel<RoomResponse>>> GetRoomByFloorAndArea
            ([FromRoute] int floorId, [FromRoute] int areaId, [FromQuery] PagingRequest paging)
        {
            try
            {
                return await _roomService.GetRoomsByFloorAndArea(floorId, areaId, paging);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }
    }
}
