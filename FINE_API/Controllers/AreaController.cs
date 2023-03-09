﻿using System.Net.NetworkInformation;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Area;
using FINE.Service.DTO.Response;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiVersion)]
    [ApiController]
    public class AreaController : ControllerBase
    {
        private readonly IAreaService _areaService;

        public AreaController(IAreaService areaService)
        {
            _areaService = areaService;
        }

        /// <summary>
        /// Get List Areas    
        /// </summary>
       
        [HttpGet]
        public async Task<ActionResult<BaseResponsePagingViewModel<AreaResponse>>> GetAreas([FromQuery] AreaResponse request, [FromQuery] PagingRequest paging)
        {
            try
            {
                return await _areaService.GetAreas(request, paging);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get Area By Id
        /// </summary>
       
        [HttpGet("{areaId}")]
        public async Task<ActionResult<BaseResponseViewModel<AreaResponse>>> GetAreaById([FromRoute] int Id)
        {
            return await _areaService.GetAreaById(Id);
        }


      
        /// <sumary>
        /// Get Area By campusID
        /// </sumary>
        [HttpGet("campus/{campusId}")]
        public async Task<ActionResult<BaseResponsePagingViewModel<AreaResponse>>> GetAreaCampusById
              ([FromRoute] int campusId, [FromQuery] PagingRequest paging)
        {
            return await _areaService.GetAreaByCampusId(campusId, paging);
        }
    }
}