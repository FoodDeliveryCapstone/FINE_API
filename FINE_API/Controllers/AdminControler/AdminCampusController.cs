using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Campus;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiAdminVersion + "/campus")]
    [ApiController]
    public class AdminCampusController : ControllerBase
    {
        private readonly ICampusService _CampusService;

        public AdminCampusController(ICampusService CampusService)
        {
            _CampusService = CampusService;
        }

        /// <summary>
        /// Get List Campus
        /// </summary>
        [Authorize(Roles = "SystemAdmin")]
        [HttpGet]
        public async Task<ActionResult<BaseResponsePagingViewModel<CampusResponse>>> GetListCampus([FromQuery] CampusResponse request, [FromQuery] PagingRequest paging)
        {
            try
            {
                return await _CampusService.GetListCampus(request, paging);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get Campus By Id
        /// </summary>
        [Authorize(Roles = "SystemAdmin")]
        [HttpGet("{campusId}")]
        public async Task<ActionResult<BaseResponseViewModel<CampusResponse>>> GetCampusById([FromRoute] int campusId)
        {
            try
            {
                return await _CampusService.GetCampusById(campusId);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Create Product
        /// </summary>
        [Authorize(Roles = "SystemAdmin")]
        [HttpPost]
        public async Task<ActionResult<BaseResponseViewModel<CampusResponse>>> CreateCampus([FromBody] CreateCampusRequest request)
        {
            try
            {
                return await _CampusService.CreateCampus(request);
            }
            catch(ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }


        /// <summary>
        /// Update Campus
        /// </summary>
        [Authorize(Roles = "SystemAdmin")]
        [HttpPut("{campusId}")]
        public async Task<ActionResult<BaseResponseViewModel<CampusResponse>>> UpdateCampus([FromRoute] int campusId, [FromBody] UpdateCampusRequest request)
        {
            try
            {
                return await _CampusService.UpdateCampus(campusId, request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }
    }
}
