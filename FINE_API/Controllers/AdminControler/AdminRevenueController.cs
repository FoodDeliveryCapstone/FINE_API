using FINE.Service.DTO.Request;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers.AdminControler
{
    [Route(Helpers.SettingVersionApi.ApiAdminVersion + "/revenue")]
    [ApiController]
    public class AdminRevenueController : ControllerBase
    {
        private readonly IRevenueService _revenueService;

        public AdminRevenueController(IRevenueService revenueService)
        {
            _revenueService = revenueService;
        }

        /// <summary>
        /// Get System Revenue
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        [HttpGet("system")]
        public async Task<ActionResult<BaseResponseViewModel<RevenueResponse>>> GetTotalRevenue([FromQuery] DateFilter filter)
        {
            return await _revenueService.GetSystemRevenueByMonth(filter);
        }

        /// <summary>
        /// Get Store Revenue
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        [HttpGet("store/{storeId}")]
        public async Task<ActionResult<BaseResponseViewModel<StoreRevenueResponse>>> GetStoreRevenue(int storeId, [FromQuery] DateFilter filter)
        {
            return await _revenueService.GetStoreRevenueByMonth(storeId, filter);
        }
    }
}
