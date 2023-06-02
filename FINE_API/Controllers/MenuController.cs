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

        /// <summary>
        /// Get List Menu
        /// </summary>

        [HttpGet]
        public async Task<ActionResult<BaseResponsePagingViewModel<MenuResponse>>> GetMenus([FromQuery] MenuResponse request, [FromQuery] PagingRequest paging)
        {
            try
            {
                return await _menuService.GetMenus(request, paging);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Get Menu By Id
        /// </summary>
   
        [HttpGet("{menuId}")]
        public async Task<ActionResult<BaseResponseViewModel<MenuResponse>>> GetMenuById([FromRoute] int menuId)
        {
            try
            {
                return await _menuService.GetMenuById(menuId);
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
        public async Task<ActionResult<BaseResponsePagingViewModel<MenuResponse>>> GetMenusByTimeslot([FromRoute] int timeslotId, [FromQuery] PagingRequest paging)
        {
            try
            {
                return await _menuService.GetMenuByTimeslot(timeslotId, paging);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }
    }
}
