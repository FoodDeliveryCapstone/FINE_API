using FINE.Service.Caches;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Order;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Mvc;
using static FINE.Service.Helpers.Enum;

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
        /// Get order by Id
        /// </summary>
        [HttpGet("{orderId}")]
        public async Task<ActionResult<BaseResponseViewModel<OrderResponse>>> GetOrderById(string orderId)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (customerId == null)
                {
                    return Unauthorized();
                }

                return await _orderService.GetOrderById(customerId, orderId);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Get CoOrder
        /// </summary>
        [HttpGet("coOrder/{partyCode}")]
        public async Task<ActionResult<BaseResponseViewModel<CoOrderResponse>>> GetPartyOrder(string partyCode)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (customerId == null)
                {
                    return Unauthorized();
                }
                var rs = await _orderService.GetPartyOrder(partyCode);
                return Ok(rs);
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
        public async Task<ActionResult<BaseResponseViewModel<OrderResponse>>> CreatePreOrder([FromBody] CreatePreOrderRequest request)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (customerId == null)
                {
                    return Unauthorized();
                }

                //var customerId = "3D596DBF-E43E-45E6-85DD-50CD1095E362";
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
        public async Task<ActionResult<BaseResponseViewModel<OrderResponse>>> CreateOrder([FromBody] CreateOrderRequest request)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (customerId == null)
                {
                    return Unauthorized();
                }
                //var customerId = "3D596DBF-E43E-45E6-85DD-50CD1095E362";
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
        [HttpPost("coOrder/active")]
        public async Task<ActionResult<BaseResponseViewModel<CoOrderResponse>>> OpenCoOrder([FromBody] CreatePreOrderRequest request)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (customerId == null)
                {
                    return Unauthorized();
                }
                //var customerId = "3D596DBF-E43E-45E6-85DD-50CD1095E362";
                return await _orderService.OpenCoOrder(customerId, request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Prepare CoOrder
        /// </summary>
        [HttpPost("coOrder/preOrder")]
        public async Task<ActionResult<BaseResponseViewModel<OrderResponse>>> CreatePreCoOrder(string timeSlot, string partyCode)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (customerId == null)
                {
                    return Unauthorized();
                }
                //var customerId = "3D596DBF-E43E-45E6-85DD-50CD1095E362";
                return await _orderService.CreatePreCoOrder(customerId, timeSlot, partyCode);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Join CoOrder
        /// </summary>
        [HttpPost("coOrder/party")]
        public async Task<ActionResult<BaseResponseViewModel<CoOrderResponse>>> JoinCoOrder(string partyCode)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (customerId == null)
                {
                    return Unauthorized();
                }
                //var customerId = "3D596DBF-E43E-45E6-85DD-50CD1095E362";
                return await _orderService.JoinPartyOrder(customerId, partyCode);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Add product into CoOrder
        /// </summary>
        [HttpPost("coOrder/card")]
        public async Task<ActionResult<BaseResponseViewModel<CoOrderResponse>>> AddProductIntoPartyCode(string partyCode, CreatePreOrderRequest request)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (customerId == null)
                {
                    return Unauthorized();
                }
                //var customerId = "3D596DBF-E43E-45E6-85DD-50CD1095E362";
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
        [HttpPost("coOrder/confirmation")]
        public async Task<ActionResult<BaseResponseViewModel<CoOrderPartyCard>>> FinalConfirmCoOrder(string partyCode)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (customerId == null)
                {
                    return Unauthorized();
                }
                //var customerId = "3D596DBF-E43E-45E6-85DD-50CD1095E362";
                return await _orderService.FinalConfirmCoOrder(customerId, partyCode);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Confirm CoOrder
        /// </summary>
        [HttpPut("coOrder/confirmation")]
        public async Task<ActionResult<BaseResponseViewModel<CoOrderResponse>>> DeletePartyOrder([FromRoute]string partyCode )
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (customerId == null)
                {
                    return Unauthorized();
                }
                var rs = await _orderService.DeletePartyOrder(customerId, partyCode);
                return Ok(rs);
            }
            catch(ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}
