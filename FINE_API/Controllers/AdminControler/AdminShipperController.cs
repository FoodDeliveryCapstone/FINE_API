using FINE.Service.DTO.Request.Order;
using FINE.Service.DTO.Request.Shipper;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers.AdminControler
{
    [Route(Helpers.SettingVersionApi.ApiAdminVersion + "/shipper")]
    [ApiController]

    public class AdminShipperController : Controller
    {
        private readonly IShipperService _shipperService;
        public AdminShipperController(IShipperService shipperService)
        {
            _shipperService = shipperService;

        }

        /// <summary>
        /// Report missing product
        /// </summary>    
        [Authorize(Roles = "SystemAdmin, StoreManager, Shipper")]
        [HttpPost("report")]
        public async Task<ActionResult<BaseResponseViewModel<ReportMissingProductResponse>>> ReportMissingProduct([FromBody] ReportMissingProductRequest request)
        {
            try
            {
                return await _shipperService.ReportMissingProductForShipper(request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }
    }
}
