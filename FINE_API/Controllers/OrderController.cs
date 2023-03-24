using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Order;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiVersion)]
    [ApiController]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Get orders
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<BaseResponsePagingViewModel<GenOrderResponse>>> GetOrders([FromQuery] PagingRequest paging)
        {
            try
            {
                return await _orderService.GetOrders(paging);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Get order by Id
        /// </summary>
        [HttpGet("orderId")]
        public async Task<ActionResult<BaseResponseViewModel<GenOrderResponse>>> GetOrderById(int orderId)
        {
            try
            {
                return await _orderService.GetOrderById(orderId);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Create PreOrder
        /// </summary>
        [HttpPost("preOrder")]
        public async Task<ActionResult<BaseResponseViewModel<GenOrderResponse>>> CreatePreOrder([FromBody] CreatePreOrderRequest request)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (customerId == -1)
                {
                    return Unauthorized();
                }

                return await _orderService.CreatePreOrder(customerId, request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Create Order
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<BaseResponseViewModel<GenOrderResponse>>> CreateOrder([FromBody] CreateGenOrderRequest request)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (customerId == -1)
                {
                    return Unauthorized();
                }
                return await _orderService.CreateOrder(customerId, request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Update Order
        /// </summary>
        [HttpPut("usercancel")]
        public async Task<ActionResult<BaseResponseViewModel<GenOrderResponse>>> UpdateCancelOrder(int orderId)
        {
            try
            {
                return await _orderService.UpdateCancelOrder(orderId);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }
    }
}
