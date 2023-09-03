using System.Net.NetworkInformation;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Order;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace FINE.API.Controllers.AdminControler
{
    [Route(Helpers.SettingVersionApi.ApiAdminVersion + "/orderDetail")]
    [ApiController]
    public class AdminOrderDetailController : ControllerBase
    {
        private readonly IOrderDetailService _orderDetailService;

        public AdminOrderDetailController(IOrderDetailService orderDetailService)
        {
            _orderDetailService = orderDetailService;
        }

        /// <summary>
        /// Get orders detail by store
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager, Shipper")]
        [HttpGet("{storeId}")]
        public async Task<ActionResult<BaseResponsePagingViewModel<OrderDetailResponse>>> GetOrderDetailByStoreId(string storeId, [FromQuery] PagingRequest paging)
        {
            try
            {
                return await _orderDetailService.GetOrdersDetailByStore(storeId, paging);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Get staff orders detail by store
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager, Shipper")]
        [HttpGet("/api/staff/orderDetail/{storeId}")]
        public async Task<ActionResult<BaseResponsePagingViewModel<OrderByStoreResponse>>> GetStaffOrderDetail(string storeId)
        {
            try
            {
                return await _orderDetailService.GetStaffOrderDetail(storeId);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Update Order by Store Status
        /// </summary>    
        [Authorize(Roles = "SystemAdmin, StoreManager, Shipper")]
        [HttpPut("status/{storeId}/{orderId}")]
        public async Task<ActionResult<BaseResponseViewModel<OrderByStoreResponse>>> UpdateOrderStatus(string storeId, string orderId, [FromBody] UpdateOrderDetailStatusRequest request)
        {
            try
            {
                return await _orderDetailService.UpdateOrderByStoreStatus(storeId, orderId, request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }
    }
}
