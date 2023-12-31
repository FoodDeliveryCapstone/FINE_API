﻿using FINE.Service.DTO.Request;
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
        [Authorize(Roles = "StoreManager")]
        [HttpGet]
        public async Task<ActionResult<BaseResponseViewModel<PackageStaffResponse>>> GetPackage(string timeSlotId)
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
        public async Task<ActionResult<BaseResponseViewModel<PackageShipperResponse>>> GetPackageForShipper(string timeSlotId)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var staffId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (staffId == null)
                {
                    return Unauthorized();
                }

                return await _packageService.GetPackageForShipper(staffId, timeSlotId);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Lấy list box group lại theo product (dành cho shipper)
        /// </summary>
        [HttpGet("listBox")]
        public async Task<ActionResult<BaseResponseViewModel<List<PackStationDetailGroupByBox>>>> GetListBoxGroupByProduct(string timeSlotId, string productId)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var staffId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (staffId == null)
                {
                    return Unauthorized();
                }
                //var staffId = "35C5D5EA-44A6-4A7C-8602-A5324FD89A6C";

                return await _packageService.GetListBoxGroupByProduct(staffId, timeSlotId, productId);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Lấy package theo store và timeSlot và group lại theo station (dành cho staff)
        /// </summary>
        //[Authorize(Roles = "StoreManager")]
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
        public async Task<ActionResult<BaseResponseViewModel<PackageStaffResponse>>> UpdatePackage([FromBody]UpdateProductPackageRequest request)
        {
            try
            {

                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var staffId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (staffId == null)
                {
                    return Unauthorized();
                }
                //var staffId = "5E67163F-80BE-4AF5-AD71-980388987695";
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
        [Authorize(Roles = "StoreManager")]
        [HttpPut("cofirmDelivery")]
        public async Task<ActionResult<BaseResponseViewModel<dynamic>>> ConfirmReadyToDelivery(string timeslotId, string stationId)
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

        /// <summary>
        /// Cập nhật package mà shipper đã lấy
        /// </summary>
        [Authorize(Roles = "Shipper")]
        [HttpPut("cofirmTaken")]
        public async Task<ActionResult<BaseResponseViewModel<dynamic>>> ConfirmTakenPackage(string timeslotId, string storeId)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var staffId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (staffId == null)
                {
                    return Unauthorized();
                }

                return await _packageService.ConfirmTakenPackage(staffId, timeslotId, storeId);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Cập nhật package mà shipper đã bỏ vào box
        /// </summary>
        [Authorize(Roles = "Shipper")]
        [HttpPut("confirmAllBox")]
        public async Task<ActionResult<BaseResponseViewModel<dynamic>>> ConfirmAllInBox(string timeslotId)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var staffId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (staffId == null)
                {
                    return Unauthorized();
                }

                return await _packageService.ConfirmAllInBox(staffId, timeslotId);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Cập nhật product đã hết hàng
        /// </summary>
        [HttpPut("reportProductCannotRepair")]
        public async Task<ActionResult<BaseResponseViewModel<dynamic>>> ReportProductCannotRepair(string timeslotId, string productId, SystemRoleTypeEnum memType)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var staffId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (staffId == null)
                {
                    return Unauthorized();
                }

                return await _packageService.ReportProductCannotRepair(staffId, timeslotId, productId, memType);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }
    }
}
