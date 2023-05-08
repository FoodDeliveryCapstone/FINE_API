using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Campus;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiVersion)]
    [ApiController]
    public class CampusController : ControllerBase
    {
        private readonly ICampusService _campusService;

        public CampusController(ICampusService campusService)
        {
            _campusService = campusService;
        }

        /// <summary>
        /// Get List Campus
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<BaseResponsePagingViewModel<CampusResponse>>> GetListCampus([FromQuery] CampusResponse request, [FromQuery] PagingRequest paging)
        {
            try
            {
                return await _campusService.GetListCampus(request, paging);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get Campus By Id
        /// </summary>
        [HttpGet("{campusId}")]
        public async Task<ActionResult<BaseResponseViewModel<CampusResponse>>> GetCampusById([FromRoute] int campusId)
        {
            try
            {
                return await _campusService.GetCampusById(campusId);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

   
    }
}
