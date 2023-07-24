//using System.Net.NetworkInformation;
//using FINE.Service.DTO.Request;
//using FINE.Service.DTO.Request.Area;
//using FINE.Service.DTO.Response;
//using FINE.Service.Service;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;

//namespace FINE.API.Controllers.AdminController
//{
//    [Route(Helpers.SettingVersionApi.ApiAdminVersion + "/area")]
//    [ApiController]
//    public class AdminAreaController : ControllerBase
//    {
//        private readonly IAreaService _areaService;

//        public AdminAreaController(IAreaService areaService)
//        {
//            _areaService = areaService;
//        }

//        /// <summary>
//        /// Get List Areas    
//        /// </summary>
//        [Authorize(Roles = "SystemAdmin")]
//        [HttpGet]
//        public async Task<ActionResult<BaseResponsePagingViewModel<AreaResponse>>> GetAreas([FromQuery] AreaResponse request, [FromQuery] PagingRequest paging)
//        {
//            try
//            {
//                return await _areaService.GetAreas(request, paging);
//            }
//            catch (Exception ex)
//            {
//                return BadRequest(ex.Message);
//            }
//        }

//        /// <summary>
//        /// Get Area By Id
//        /// </summary>
//        [Authorize(Roles = "SystemAdmin")]
//        [HttpGet("{areaId}")]
//        public async Task<ActionResult<BaseResponseViewModel<AreaResponse>>> GetAreaById([FromRoute] int Id)
//        {
//            return await _areaService.GetAreaById(Id);
//        }

//        /// <summary>
//        /// Create                        
//        /// </summary>
//        [Authorize(Roles = "SystemAdmin")]
//        [HttpPost]
//        public async Task<ActionResult<BaseResponseViewModel<AreaResponse>>> CreateArea([FromBody] CreateAreaRequest request)
//        {
//            return await _areaService.CreateArea(request);
//        }

//        /// <summary>
//        /// Update
//        /// </summary>
//        [Authorize(Roles = "SystemAdmin")]
//        [HttpPut("{areaId}")]
//        public async Task<ActionResult<BaseResponseViewModel<AreaResponse>>> UpdateArea([FromRoute] int areaId, [FromBody] UpdateAreaRequest request)
//        {
//            return await _areaService.UpdateArea(areaId, request);
//        }
//        /// <sumary>
//        /// Get Area By DestinationID
//        /// </sumary>
//        [Authorize(Roles = "SystemAdmin")]
//        [HttpGet("Destination/{DestinationId}")]
//        public async Task<ActionResult<BaseResponsePagingViewModel<AreaResponse>>> GetAreaDestinationById
//              ([FromRoute] int DestinationId, [FromQuery] PagingRequest paging)
//        {
//            return await _areaService.GetAreaByDestinationId(DestinationId, paging);
//        }
//    }
//}
