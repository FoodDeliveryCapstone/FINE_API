using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Staff;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers.AdminStaffController
{
    [Route(Helpers.SettingVersionApi.ApiAdminVersion + "/staff")]
    [ApiController]

    public class AdminStaffController : Controller
    {
        private readonly IStaffService _staffService;
        public AdminStaffController(IStaffService staffService)
        {
            _staffService = staffService;
        }

        ///// <summary>
        ///// Get List Staff    
        ///// </summary>
        //[Authorize(Roles = "SystemAdmin, StoreManager")]
        //[HttpGet]
        //public async Task<ActionResult<BaseResponsePagingViewModel<StaffResponse>>> GetStaffs
        //    ([FromQuery] StaffResponse request, [FromQuery] PagingRequest paging)
        //{
        //    try
        //    {
        //        return await _staffService.GetStaffs(request, paging);
        //    }
        //    catch (ErrorResponse ex)
        //    {
        //        return BadRequest(ex.Error);
        //    }
        //}

        /// <summary>
        /// Get Staff By Id
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        [HttpGet("{staffId}")]
        public async Task<ActionResult<BaseResponseViewModel<StaffResponse>>> GetStaffById
            ([FromRoute] string staffId)
        {
            return await _staffService.GetStaffById(staffId);
        }

        /// <summary>
        /// Create Admin Account                        
        /// </summary>
        [Authorize(Roles = "SystemAdmin")]
        [HttpPost]
        public async Task<ActionResult<BaseResponseViewModel<StaffResponse>>> CreateAdminManager([FromBody] CreateStaffRequest request)
        {
            try
            {
                return await _staffService.CreateAdminManager(request);
            }
            catch (ErrorResponse ex) { 
                return BadRequest(ex.Error); 
            }
        }

        ///// <summary>
        ///// Update Staff 
        ///// </summary>
        //[Authorize(Roles = "SystemAdmin, StoreManager")]
        //[HttpPut("{staffId}")]
        //public async Task<ActionResult<BaseResponseViewModel<StaffResponse>>> UpdateStaff
        //    ([FromRoute] int staffId, [FromBody] UpdateStaffRequest request)
        //{
        //    return await _staffService.UpdateStaff(staffId, request);
        //}

    }
}
