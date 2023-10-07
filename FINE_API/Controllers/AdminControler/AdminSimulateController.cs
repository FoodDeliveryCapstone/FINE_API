using FINE.Service.DTO.Request.Order;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers.AdminControler
{
    [Route(Helpers.SettingVersionApi.ApiAdminVersion + "/simulate")]
    [ApiController]
    public class AdminSimulateController : Controller
    {
        private readonly ISimulateService _simulateService;
        public AdminSimulateController(ISimulateService simulateService)
        {
            _simulateService = simulateService;
        }

        /// <summary>
        /// Simulate Order
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        [HttpPost("order")]
        public async Task<ActionResult<BaseResponseViewModel<SimulateResponse>>> SimulateOrder([FromBody] SimulateRequest request)
        {
            try
            {
                return await _simulateService.SimulateOrder(request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Simulate Order Status to Finish
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        [HttpPut("status/finish")]
        public async Task<ActionResult<BaseResponsePagingViewModel<SimulateOrderStatusResponse>>> SimulateOrderStatusToFinish([FromBody] SimulateOrderStatusRequest request)
        {
            try
            {
                return await _simulateService.SimulateOrderStatusToFinish(request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Simulate Order Status to BoxStored
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        [HttpPut("status/boxStored")]
        public async Task<ActionResult<BaseResponsePagingViewModel<SimulateOrderStatusResponse>>> SimulateOrderStatusToBoxStored([FromBody] SimulateOrderStatusRequest request)
        {
            try
            {
                return await _simulateService.SimulateOrderStatusToBoxStored(request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Simulate Order Status to Finish Prepare
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        [HttpPut("status/finishPrepare")]
        public async Task<ActionResult<BaseResponsePagingViewModel<SimulateOrderStatusResponse>>> SimulateOrderStatusToFinishPrepare([FromBody] SimulateOrderStatusRequest request)
        {
            try
            {
                return await _simulateService.SimulateOrderStatusToFinishPrepare(request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Simulate Order Status to Delivering
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        [HttpPut("status/delivering")]
        public async Task<ActionResult<BaseResponsePagingViewModel<SimulateOrderStatusResponse>>> SimulateOrderStatusToDelivering([FromBody] SimulateOrderStatusRequest request)
        {
            try
            {
                return await _simulateService.SimulateOrderStatusToDelivering(request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }
    }
}
