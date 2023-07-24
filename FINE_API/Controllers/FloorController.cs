﻿//using System.Net.NetworkInformation;
//using FINE.Service.DTO.Request;
//using FINE.Service.DTO.Request.Menu;
//using FINE.Service.DTO.Request.Product;
//using FINE.Service.DTO.Response;
//using FINE.Service.Exceptions;
//using FINE.Service.Service;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;

//namespace FINE.API.Controllers
//{
//    [Route(Helpers.SettingVersionApi.ApiVersion)]
//    [ApiController]
//    public class FloorController : ControllerBase
//    {
//        private readonly IFloorService _floorService;

//        public FloorController(IFloorService floorService)
//        {
//            _floorService = floorService;
//        }
     

//        /// <summary>
//        /// Get Floor by DestinationId 
//        /// </summary>

//        [HttpGet("Destination/{DestinationId}")]
//        public async Task<ActionResult<BaseResponsePagingViewModel<FloorResponse>>> GetFloorByDestination
//            ([FromRoute] int DestinationId, [FromQuery] PagingRequest paging)
//        {
//            try
//            {
//                return await _floorService.GetFloorsByDestination(DestinationId, paging);
//            }
//            catch (ErrorResponse ex)
//            {
//                return BadRequest(ex.Error);
//            }
//        }
//    }
//}
