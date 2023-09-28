﻿using System.Net.NetworkInformation;
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
        /// Get split orders detail by store and station
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager, Shipper")]
        [HttpGet("staff/{storeId}/{stationId}")]
        public async Task<ActionResult<BaseResponsePagingViewModel<OrderByStoreResponse>>> GetSplitOrderDetail(string storeId, string stationId, int status, string? timeslotId)
        {
            try
            {
                return await _orderDetailService.GetSplitOrderDetail(storeId, stationId, status, timeslotId);
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
        [HttpPut("status/storeId/orderId")]
        public async Task<ActionResult<BaseResponseViewModel<OrderByStoreResponse>>> UpdateOrderStatus([FromBody] UpdateOrderDetailStatusRequest request)
        {
            try
            {
                return await _orderDetailService.UpdateOrderByStoreStatus(request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Get order details by order
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager, Shipper")]
        [HttpGet("{orderId}")]
        public async Task<ActionResult<BaseResponsePagingViewModel<OrderByStoreResponse>>> GetOrderDetailByOrderId(string orderId)
        {
            try
            {
                return await _orderDetailService.GetStaffOrderDetailByOrderId(orderId);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }


        /// <summary>
        /// Get split orders by store 
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager, Shipper")]
        [HttpGet("splitOrder/{storeId}")]
        public async Task<ActionResult<BaseResponsePagingViewModel<SplitOrderResponse>>> GetSplitOrder(string storeId, string timeslotId, int status,string? stationId)
        {
            try
            {
                return await _orderDetailService.GetSplitOrder(storeId, timeslotId, status, stationId);
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
