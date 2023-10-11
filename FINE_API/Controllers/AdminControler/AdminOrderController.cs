using System.Net.NetworkInformation;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Order;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static FINE.Service.Helpers.Enum;

namespace FINE.API.Controllers.AdminControler
{
    [Route(Helpers.SettingVersionApi.ApiAdminVersion + "/order")]
    [ApiController]
    public class AdminOrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IStaffService _staffService;

        public AdminOrderController(IOrderService orderService, IStaffService staffService)
        {
            _orderService = orderService;
            _staffService = staffService;
        }

        /// <summary>
        /// Get orders for staff
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        [HttpGet]
        public async Task<ActionResult<BaseResponsePagingViewModel<OrderForStaffResponse>>> GetOrdersForStaff([FromQuery]OrderForStaffResponse filter, [FromQuery] PagingRequest paging)
        {
            try
            {
                return await _orderService.GetOrders(filter,paging);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        ///// <summary>
        ///// Get CoOrder
        ///// </summary>
        //[Authorize(Roles = "SystemAdmin, StoreManager")]
        //[HttpGet("coOrder/{partyCode}")]
        //public async Task<ActionResult<BaseResponseViewModel<CoOrderResponse>>> GetPartyOrder(string partyCode)
        //{
        //    try
        //    {
        //        var rs = await _orderService.GetPartyOrder(partyCode);
        //        return Ok(rs);
        //    }
        //    catch (ErrorResponse ex)
        //    {
        //        return BadRequest(ex.Error);
        //    }
        //}

        /// <summary>
        /// Update Order Status
        /// </summary>    
        [Authorize(Roles = "SystemAdmin, StoreManager, Shipper")]
        [HttpPut("status/{orderId}")]
        public async Task<ActionResult<BaseResponseViewModel<OrderResponse>>> UpdateOrderStatus([FromRoute] string orderId, [FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                return await _staffService.UpdateOrderStatus(orderId, request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }
    }
}

