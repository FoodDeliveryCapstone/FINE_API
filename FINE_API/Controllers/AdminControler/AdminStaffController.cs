using FINE.Data.Entity;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Box;
using FINE.Service.DTO.Request.Staff;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Drawing.Imaging;

namespace FINE.API.Controllers.AdminStaffController
{
    [Route(Helpers.SettingVersionApi.ApiAdminVersion + "/staff")]
    [ApiController]

    public class AdminStaffController : Controller
    {
        private readonly IStaffService _staffService;
        private readonly IQrCodeService _qrCodeService;
        public AdminStaffController(IStaffService staffService, IQrCodeService qrCodeService)
        {
            _staffService = staffService;
            _qrCodeService = qrCodeService;
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
        /// Get Staff info by token
        /// </summary>
        [HttpGet("authorization")]
        public async Task<ActionResult<StaffResponse>> GetStaffByToken()
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var staffId = FireBaseService.GetUserIdFromHeaderToken(accessToken);
                if (staffId == null)
                {
                    return Unauthorized();
                }
                var result = await _staffService.GetStaffById(staffId);
                return Ok(result);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
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

        /// <summary>
        /// Get QR for shipper by boxId
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager, Shipper")]
        [HttpPost("qrCode")]
        public async Task<ActionResult> GetQRCode([FromBody] List<AddOrderToBoxRequest> request)
        {
            try
            {
                var qrCodeBitmap = await _qrCodeService.GenerateShipperQrCode(request);
                using (MemoryStream stream = new MemoryStream())
                {
                    qrCodeBitmap.Save(stream, ImageFormat.Png);
                    byte[] imageBytes = stream.ToArray();

                    return File(imageBytes, "image/png");
                }
            } catch (ErrorResponse ex)
            {
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
