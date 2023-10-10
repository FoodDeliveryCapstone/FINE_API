using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Store;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers.StaffControllers
{
    [Route(Helpers.SettingVersionApi.ApiStaffVersion)]
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
        public async Task<ActionResult<BaseResponseViewModel<PackageResponse>>> GetOrderById(string storeId, string timeSlotId)
        {
            try
            {
                return await _packageService.GetPackage(storeId, timeSlotId);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

    }
}
