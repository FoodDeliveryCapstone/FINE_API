using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Destination;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiAdminVersion + "/destination")]
    [ApiController]
    public class AdminDestinationController : ControllerBase
    {
        private readonly IDestinationService _DestinationService;

        public AdminDestinationController(IDestinationService DestinationService)
        {
            _DestinationService = DestinationService;
        }

        ///// <summary>
        ///// Get List Destination
        ///// </summary>
        //[Authorize(Roles = "SystemAdmin")]
        //[HttpGet]
        //public async Task<ActionResult<BaseResponsePagingViewModel<DestinationResponse>>> GetListDestination([FromQuery] DestinationResponse request, [FromQuery] PagingRequest paging)
        //{
        //    try
        //    {
        //        return await _DestinationService.GetListDestination(request, paging);

        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //}

        /// <summary>
        /// Get Destination By Id
        /// </summary>
        [Authorize(Roles = "SystemAdmin")]
        [HttpGet("{id}")]
        public async Task<ActionResult<BaseResponseViewModel<DestinationResponse>>> GetDestinationById([FromRoute] string id)
        {
            try
            {
                return await _DestinationService.GetDestinationById(id);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Create Destination
        /// </summary>
        //[Authorize(Roles = "SystemAdmin")]
        [HttpPost]
        public async Task<ActionResult<BaseResponseViewModel<DestinationResponse>>> CreateDestination([FromBody] CreateDestinationRequest request)
        {
            try
            {
                return await _DestinationService.CreateDestination(request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }


        /// <summary>
        /// Update Destination
        /// </summary>
        //[Authorize(Roles = "SystemAdmin")]
        [HttpPut("{id}")]
        public async Task<ActionResult<BaseResponseViewModel<DestinationResponse>>> UpdateDestination([FromRoute] string id, [FromBody] UpdateDestinationRequest request)
        {
            try
            {
                return await _DestinationService.UpdateDestination(id, request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }
    }
}
