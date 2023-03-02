using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Campus;
using FINE.Service.DTO.Response;
using FINE.Service.Service;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers
{
    [Route("api/campus")]
    [ApiController]
    public class CampusController : ControllerBase
    {
        private readonly ICampusService _CampusService;

        public CampusController(ICampusService CampusService)
        {
            _CampusService = CampusService;
        }

        /// <summary>
        /// Get List Campus
        /// </summary>
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
        [HttpGet("{campusId}")]
        public async Task<ActionResult<BaseResponseViewModel<CampusResponse>>> GetCampusById([FromRoute] int CampusId)
        {
            return await _CampusService.GetCampusById(CampusId);
        }

        /// <summary>
        /// Create Product
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<BaseResponseViewModel<CampusResponse>>> CreateCampus([FromBody] CreateCampusRequest request)
        {
            return await _CampusService.CreateCampus(request);
        }


        /// <summary>
        /// Update Campus
        /// </summary>
        [HttpPut("{campusId}")]
        public async Task<ActionResult<BaseResponseViewModel<CampusResponse>>> UpdateCampus([FromRoute] int CampusId, [FromBody] UpdateCampusRequest request)
        {
            return await _CampusService.UpdateCampus(CampusId, request);
        }
    }
}
