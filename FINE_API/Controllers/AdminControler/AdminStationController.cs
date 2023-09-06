﻿using FINE.Service.DTO.Request;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiAdminVersion + "/station")]
    [ApiController]
    public class AdminStationController : ControllerBase
    {
        private readonly IStationService _stationService;

        public AdminStationController(IStationService stationService)
        {
            _stationService = stationService;
        }

        /// <summary>
        /// Get Station by Id
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager, Shipper")]
        [HttpGet("{stationId}")]
        public async Task<ActionResult<BaseResponseViewModel<StationResponse>>> GetStationById([FromRoute] string stationId)
        {
            try
            {
                var result = await _stationService.GetStationById(stationId);
                return Ok(result);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Get Stations
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager, Shipper")]
        [HttpGet]
        public async Task<ActionResult<BaseResponsePagingViewModel<StationResponse>>> GetStations([FromQuery] StationResponse filter, [FromQuery] PagingRequest paging)
        {
            try
            {
                var result = await _stationService.GetStations(filter, paging);
                return Ok(result);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }


    }
}
