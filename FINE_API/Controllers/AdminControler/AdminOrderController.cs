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

        /// <summary>
        /// Open CoOrder
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        [HttpPost("coOrder/active")]
        public async Task<ActionResult<BaseResponseViewModel<CoOrderResponse>>> OpenCoOrder([FromQuery] string customerId, [FromBody] CreatePreOrderRequest request)
        {
            try
            {
                return await _orderService.OpenCoOrder(customerId, request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Join CoOrder
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        [HttpPut("coOrder/party")]
        public async Task<ActionResult<BaseResponseViewModel<CoOrderResponse>>> JoinCoOrder([FromQuery] string customerId, string partyCode)
        {
            try
            {
                return await _orderService.JoinPartyOrder(customerId, partyCode);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Get CoOrder
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        [HttpGet("coOrder/{partyCode}")]
        public async Task<ActionResult<BaseResponseViewModel<CoOrderResponse>>> GetPartyOrder(string partyCode)
        {
            try
            {
                var rs = await _orderService.GetPartyOrder(partyCode);
                return Ok(rs);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Add product into CoOrder
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        [HttpPost("coOrder/card")]
        public async Task<ActionResult<BaseResponseViewModel<CoOrderResponse>>> AddProductIntoPartyCode([FromQuery] string customerId, string partyCode, CreatePreOrderRequest request)
        {
            try
            {
                return await _orderService.AddProductIntoPartyCode(customerId, partyCode, request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Confirm CoOrder
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        [HttpPut("coOrder/confirmation")]
        public async Task<ActionResult<BaseResponseViewModel<CoOrderPartyCard>>> FinalConfirmCoOrder([FromQuery] string customerId, string partyCode)
        {
            try
            {
                return await _orderService.FinalConfirmCoOrder(customerId, partyCode);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Prepare CoOrder
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        [HttpPost("coOrder/preOrder")]
        public async Task<ActionResult<BaseResponseViewModel<OrderResponse>>> CreatePreCoOrder([FromQuery] string customerId, int orderType, string partyCode)
        {
            try
            {
                return await _orderService.CreatePreCoOrder(customerId, orderType, partyCode);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Update Order Status
        /// </summary>    
        [Authorize(Roles = "SystemAdmin, StoreManager, Shipper")]
        [HttpPut("status/{orderId}")]
        public async Task<ActionResult<BaseResponseViewModel<OrderResponse>>> UpdateOrderStatus([FromRoute] string orderId, [FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                return await _orderService.UpdateOrderStatus(orderId, request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Simulate Order
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        [HttpPost("simulate/order")]
        public async Task<ActionResult<BaseResponseViewModel<SimulateResponse>>> SimulateOrder([FromQuery] SimulateRequest request)
        {
            try
            {
                return await _orderService.SimulateOrder(request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }


    }
}

