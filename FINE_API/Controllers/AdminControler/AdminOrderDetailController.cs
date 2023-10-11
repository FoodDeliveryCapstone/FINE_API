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
        [HttpGet("all/{storeId}")]
        public async Task<ActionResult<BaseResponsePagingViewModel<OrderDetailResponse>>> GetOrderDetailByStoreId(string storeId, int orderStatus, [FromQuery] PagingRequest paging)
        {
            try
            {
                return await _orderDetailService.GetOrdersDetailByStore(storeId, orderStatus, paging);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Get shipper split orders
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager, Shipper")]
        [HttpGet("shipper/splitOrder/{timeslotId}/{stationId}")]
        public async Task<ActionResult<BaseResponsePagingViewModel<ShipperSplitOrderResponse>>> GetShipperSplitOrder(string timeslotId, string stationId, string? storeId, int? status)
        {
            try
            {
                return await _orderDetailService.GetShipperSplitOrder(timeslotId, stationId, storeId, status);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Get shipper order box
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager, Shipper")]
        [HttpGet("orderBox/{timeslotId}/{stationId}")]
        public async Task<ActionResult<BaseResponsePagingViewModel<ShipperOrderBoxResponse>>> GetShipperOrderBox(string stationId, string timeslotId)
        {
            try
            {
                return await _orderDetailService.GetShipperOrderBox(stationId, timeslotId);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }
    }
}

