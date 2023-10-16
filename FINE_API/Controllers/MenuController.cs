using System.Net.NetworkInformation;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Menu;
using FINE.Service.DTO.Request.Product;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiVersion)]
    [ApiController]
    public class MenuController : ControllerBase
    {
        private readonly IMenuService _menuService;

        public MenuController(IMenuService menuService)
        {
            _menuService = menuService;
        }

        ///// <summary>
        ///// Get Menu By Id
        ///// </summary>

        [HttpGet("{id}")]
        public async Task<ActionResult<BaseResponseViewModel<MenuResponse>>> GetMenuById([FromRoute] string id)
        {
            try
            {
                return await _menuService.GetMenuById(id);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Get List Menu by TimeslotId
        /// </summary>

        [HttpGet("timeslot/{timeslotId}")]
        public async Task<ActionResult<BaseResponseViewModel<MenuByTimeSlotResponse>>> GetMenusByTimeslot([FromRoute] string timeslotId)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);
                if (customerId == null)
                {
                    return Unauthorized();
                }
                return await _menuService.GetMenuByTimeslot(customerId,timeslotId);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }
    }
}
