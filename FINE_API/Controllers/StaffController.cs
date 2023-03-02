using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Area;
using FINE.Service.DTO.Request.Staff;
using FINE.Service.DTO.Response;
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
            catch (Exception ex)
            {
                return BadRequest("Invalid External Authentication.");
            }
        }

        /// <summary>
        /// Get Staff By Id
        /// </summary>
        /// 
        [HttpGet("{staffId}")]
        public async Task<ActionResult<BaseResponseViewModel<StaffResponse>>> GetStaffById
            ([FromRoute] int staffId)
        {
            return await _staffService.GetStaffById(staffId);
        }

        /// <summary>
        /// Create Staff                        
        /// </summary>
        /// 
        [HttpPost]
        public async Task<ActionResult<BaseResponseViewModel<StaffResponse>>> CreateStaff
            ([FromBody] CreateStaffRequest request)
        {
            return await _staffService.CreateAdminManager(request);
        }

        /// <summary>
        /// Update Staff 
        /// </summary>
        /// 
        [HttpPut("{staffId}")]
        public async Task<ActionResult<BaseResponseViewModel<StaffResponse>>> UpdateStaff
            ([FromRoute] int staffId, [FromBody] UpdateStaffRequest request)
        {
            return await _staffService.UpdateStaff(staffId, request);
        }
    }
}
