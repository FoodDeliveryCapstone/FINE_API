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
    [Route(Helpers.SettingVersionApi.ApiAdminVersion + "/order")]
    [ApiController]
    public class AdminOrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public AdminOrderController(IOrderService orderService)
        {
            _orderService = orderService;
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

        /// <summary>
        /// Create PreOrder by Admin
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        [HttpPost("preOrder")]
        public async Task<ActionResult<BaseResponseViewModel<OrderResponse>>> CreatePreOrder([FromQuery] string customerId,[FromBody] CreatePreOrderRequest request)
        {
            try
            {
                return await _orderService.CreatePreOrder(customerId, request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Create Order by Admin
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        [HttpPost]
        public async Task<ActionResult<BaseResponseViewModel<OrderResponse>>> CreateOrder([FromQuery] string customerId,[FromBody] CreateOrderRequest request)
        {
            try
            {
                return await _orderService.CreateOrder(customerId, request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }
    }
}
