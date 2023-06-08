using System.Net.NetworkInformation;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Menu;
using FINE.Service.DTO.Request.Product;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers.AdminControler
{
    [Route(Helpers.SettingVersionApi.ApiAdminVersion + "/menu")]
    [ApiController]
    public class AdminMenuController : ControllerBase
    {
        private readonly IMenuService _menuService;

        public AdminMenuController(IMenuService menuService)
        {
            _menuService = menuService;
        }

        /// <summary>
        /// Get List Menu
        /// </summary>
        [Authorize(Roles = "SystemAdmin")]
        [HttpGet]
        public async Task<ActionResult<BaseResponsePagingViewModel<MenuWithoutProductResponse>>> GetMenus([FromQuery] MenuWithoutProductResponse request, [FromQuery] PagingRequest paging)
        {
            try
            {
                return await _menuService.GetMenus(request, paging);
            }
            catch(ErrorResponse ex) 
            { 
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Get Menu By Id
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        [HttpGet("{menuId}")]
        public async Task<ActionResult<BaseResponseViewModel<MenuResponse>>> GetMenuById([FromRoute] int menuId)
        {
            try
            {
                return await _menuService.GetMenuById(menuId);
            }
            catch(ErrorResponse ex) 
            { 
                return BadRequest(ex.Error); 
            }
        }

        /// <summary>
        /// Get List Menu by TimeslotId
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        [HttpGet("timeslot/{timeslotId}")]
        public async Task<ActionResult<BaseResponsePagingViewModel<MenuResponse>>> GetMenusByTimeslot([FromRoute] int timeslotId, [FromQuery] PagingRequest paging)
        {
            try
            {
                return await _menuService.GetMenuByTimeslot(timeslotId, paging);
            }
            catch(ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Create Menu
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        public async Task<ActionResult<BaseResponseViewModel<MenuResponse>>> CreateMenu([FromBody] CreateMenuRequest request)
        {
            try
            {
                return await _menuService.CreateMenu(request);
            }
            catch(ErrorResponse ex) 
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Update Menu
        /// </summary>    
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        [HttpPut("{menuId}")]
        public async Task<ActionResult<BaseResponseViewModel<MenuResponse>>> UpdateMenu([FromRoute] int menuId, [FromBody] UpdateMenuRequest request)
        {
            try
            {
                return await _menuService.UpdateMenu(menuId, request);
            }
            catch(ErrorResponse ex)
            { 
                return BadRequest(ex.Error);
            }
        }
    }
}
