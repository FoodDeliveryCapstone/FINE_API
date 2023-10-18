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
        /// Lấy order bằng id
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
        /// fetch status order
        /// </summary>
        [HttpGet("status/{orderId}")]
        public async Task<ActionResult<BaseResponseViewModel<dynamic>>> GetOrderStatusByOrderId(string orderId)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (customerId == null)
                {
                    return Unauthorized();
                }

                return await _orderService.GetOrderStatus(orderId);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Lấy CoOrder 
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
                var rs = await _orderService.GetPartyOrder(customerId, partyCode);
                return Ok(rs);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Lấy CoOrder status
        /// </summary>
        [HttpGet("coOrder/status/{partyCode}")]
        public async Task<ActionResult<BaseResponseViewModel<CoOrderStatusResponse>>> GetPartyStatus(string partyCode)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (customerId == null)
                {
                    return Unauthorized();
                }
                var rs = await _orderService.GetPartyStatus(partyCode);
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
                //var customerId = "6E9C0199-44E4-4A60-9037-4B04CBE2D12D";
                return await _orderService.CreatePreOrder(customerId, request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Create PreOrder for reOrder
        /// </summary>
        [HttpPost("reOrder")]
        public async Task<ActionResult<BaseResponseViewModel<CreateReOrderResponse>>> CreatePreOrderFromReOrder(string orderId, OrderTypeEnum orderType)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (customerId == null)
                {
                    return Unauthorized();
                }
                //var customerId = "3EACFBD9-FEBC-4E8F-BE0B-66932C67CBD4";

                return await _orderService.CreatePreOrderFromReOrder(customerId, orderId, orderType);
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
                //var customerId = "6E9C0199-44E4-4A60-9037-4B04CBE2D12D";
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

                return await _orderService.OpenParty(customerId, request);
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
        public async Task<ActionResult<BaseResponseViewModel<OrderResponse>>> CreatePreCoOrder(OrderTypeEnum orderType, string partyCode)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (customerId == null)
                {
                    return Unauthorized();
                }

                return await _orderService.CreatePreCoOrder(customerId, orderType, partyCode);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Join CoOrder
        /// </summary>
        [HttpPut("coOrder/party")]
        public async Task<ActionResult<BaseResponseViewModel<CoOrderResponse>>> JoinCoOrder(string timeSlotId, string partyCode)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (customerId == null)
                {
                    return Unauthorized();
                }

                return await _orderService.JoinPartyOrder(customerId, timeSlotId, partyCode);
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
        public async Task<ActionResult<BaseResponseViewModel<CoOrderResponse>>> AddProductIntoPartyCode(string partyCode, [FromBody] CreatePreOrderRequest request)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (customerId == null)
                {
                    return Unauthorized();
                }

                return await _orderService.AddProductIntoPartyCode(customerId, partyCode, request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Add product into card
        /// </summary>
        [HttpPost("card")]
        public async Task<ActionResult<BaseResponseViewModel<AddProductToCardResponse>>> AddProductIntoCard([FromBody] AddProductToCardRequest request)
        {
            try
            {
                //var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                //var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                //if (customerId == null)
                //{
                //    return Unauthorized();
                //}

                var customerId = "3EACFBD9-FEBC-4E8F-BE0B-66932C67CBD4";

                return Ok(await _orderService.AddProductToCard(customerId, request));
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

                return await _orderService.FinalConfirmCoOrder(customerId, partyCode);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Delete CoOrder
        /// </summary>
        [HttpPut("coOrder/out")]
        public async Task<ActionResult<BaseResponseViewModel<CoOrderResponse>>> DeletePartyOrder(PartyOrderType type, string partyCode, string? memberId = null)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (customerId == null)
                {
                    return Unauthorized();
                }
                var rs = await _orderService.DeletePartyOrder(customerId, type, partyCode, memberId);
                return Ok(rs);
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Remove party member CoOrder
        /// </summary>
        [HttpPut("coOrder/member")]
        public async Task<ActionResult<BaseResponseViewModel<dynamic>>> RemovePartyMember(string partyCode, string? memberId = null)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (customerId == null)
                {
                    return Unauthorized();
                }
                var rs = await _orderService.RemovePartyMember(customerId, partyCode, memberId);
                return Ok(rs);
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}
