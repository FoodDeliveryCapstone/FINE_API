using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Package;
using FINE.Service.DTO.Request.Staff;
using FINE.Service.DTO.Request.Store;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static FINE.Service.Helpers.Enum;

namespace FINE.API.Controllers.StaffControllers
{
    [Route(Helpers.SettingVersionApi.ApiStaffVersion + "/package")]
    [ApiController]
    public class PackageController : ControllerBase
    {
        private readonly IPackageService _packageService;

        public PackageController(IPackageService packageService)
        {
            _packageService = packageService;
        }

        /// <summary>
        /// Lấy package theo store và timeSlot 
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<BaseResponseViewModel<PackageResponse>>> GetPackage(string timeSlotId)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var staffId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (staffId == null)
                {
                    return Unauthorized();
                }
                return await _packageService.GetPackage(staffId, timeSlotId);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Lấy package theo station và timeSlot và group lại theo store (dành cho shipper)
        /// </summary>
        [Authorize(Roles = "Shipper")]
        [HttpGet("deliveryPackage")]
        public async Task<ActionResult<BaseResponseViewModel<List<PackageShipperResponse>>>> GetPackageForShipper(string timeSlotId)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var staffId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (staffId == null)
                {
                    return Unauthorized();
                }
                //var staffId = "719840C7-5EA9-4A34-81ED-22E52F474CD1";
                return await _packageService.GetPackageForShipper(staffId, timeSlotId);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Lấy package theo store và timeSlot và group lại theo station (dành cho staff)
        /// </summary>
        [HttpGet("station")]
        public async Task<ActionResult<BaseResponseViewModel<List<PackageStationResponse>>>> GetPackageGroupByStation(string timeSlotId)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var staffId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (staffId == null)
                {
                    return Unauthorized();
                }
                //var staffId = "719840C7-5EA9-4A34-81ED-22E52F474CD1";
                return await _packageService.GetPackageGroupByStation(staffId, timeSlotId);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Cập nhật tình trạng sp của package
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<BaseResponseViewModel<PackageResponse>>> UpdatePackage([FromBody]UpdateProductPackageRequest request)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var staffId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (staffId == null)
                {
                    return Unauthorized();
                }
                return await _packageService.UpdatePackage(staffId, request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Cập nhật package đã sẵn sàng để giao
        /// </summary>
        [HttpPut("cofirmDelivery")]
        public async Task<ActionResult<BaseResponseViewModel<PackageResponse>>> ConfirmReadyToDelivery(string timeslotId, string stationId)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var staffId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (staffId == null)
                {
                    return Unauthorized();
                }
                return await _packageService.ConfirmReadyToDelivery(staffId, timeslotId ,stationId);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }
    }
}
