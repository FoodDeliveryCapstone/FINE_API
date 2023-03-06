﻿using System.Net.NetworkInformation;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Menu;
using FINE.Service.DTO.Request.Product;
using FINE.Service.DTO.Response;
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
        public async Task<ActionResult<BaseResponsePagingViewModel<MenuResponse>>> GetMenus([FromQuery] MenuResponse request, [FromQuery] PagingRequest paging)
        {
            return await _menuService.GetMenus(request, paging);
        }

        /// <summary>
        /// Get Menu By Id
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        [HttpGet("{menuId}")]
        public async Task<ActionResult<BaseResponseViewModel<MenuResponse>>> GetMenuById([FromRoute] int menuId)
        {
            return await _menuService.GetMenuById(menuId);
        }

        /// <summary>
        /// Get List Menu by TimeslotId
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        [HttpGet("timeslot/{timeslotId}")]
        public async Task<ActionResult<BaseResponsePagingViewModel<MenuResponse>>> GetMenusByTimeslot([FromRoute] int timeslotId, [FromQuery] PagingRequest paging)
        {
            return await _menuService.GetMenuByTimeslot(timeslotId, paging);
        }

        /// <summary>
        /// Create Menu
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        public async Task<ActionResult<BaseResponseViewModel<MenuResponse>>> CreateMenu([FromBody] CreateMenuRequest request)
        {
            return await _menuService.CreateMenu(request);
        }

        /// <summary>
        /// Update Menu
        /// </summary>    
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        [HttpPut("{menuId}")]
        public async Task<ActionResult<BaseResponseViewModel<MenuResponse>>> UpdateMenu([FromRoute] int menuId, [FromBody] UpdateMenuRequest request)
        {
            return await _menuService.UpdateMenu(menuId, request);
        }
    }
}
