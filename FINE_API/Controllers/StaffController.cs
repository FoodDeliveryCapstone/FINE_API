using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Area;
using FINE.Service.DTO.Request.Staff;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiVersion)]
    [ApiController]
    public class StaffController : ControllerBase
    {
        private readonly IStaffService _staffService;

        public StaffController(IStaffService staffService)
        {
            _staffService = staffService;
        }

        /// <summary>
        /// Login Admin
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<StaffResponse>> Login(LoginRequest request)
        {
            try
            {
                var result = await _staffService.Login(request);
                return Ok(result);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        ///// <summary>
        ///// Get all staff for system admin
        ///// </summary>
        //[HttpGet()]
        //public async Task<ActionResult<BaseResponsePagingViewModel<StaffResponse>>> GetStaffs([FromRoute] StaffResponse staffResponse, [FromRoute] PagingRequest pagingRequest)
        //{
        //    return await _staffService.GetStaffs(staffResponse, pagingRequest);
        //}
    }
}
