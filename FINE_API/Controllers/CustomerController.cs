using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Customer;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiVersion)]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;

        public CustomerController(ICustomerService customerService, IOrderService orderService)
        {
            _orderService = orderService;
            _customerService = customerService;
        }

        /// <summary>
        /// Login
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost("login")]
        public async Task<ActionResult<CustomerResponse>> Login([FromBody] ExternalAuthRequest data)
        {
            try
            {
                var result = await _customerService.Login(data);
                return Ok(result);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// lấy thông tin khách hàng bằng token
        /// </summary>
        [HttpGet("authorization")]
        public async Task<ActionResult<CustomerResponse>> GetCustomerByToken()
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);
                if (customerId == null)
                {
                    return Unauthorized();
                }
                var result = await _customerService.GetCustomerById(customerId);
                return Ok(result);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Update thông tin khách hàng
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<ActionResult<BaseResponseViewModel<CustomerResponse>>> UpdateCustomer([FromQuery] UpdateCustomerRequest request)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);
                if (customerId == null)
                {
                    return Unauthorized();
                }
                //var customerId = "4D8420A7-E159-4975-88DD-F80BCEA9AC04";
                var result = await _customerService.UpdateCustomer(customerId, request);
                return Ok(result);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// tìm khách hàng bằng số điện thoại
        /// </summary>
        [HttpGet("find")]
        public async Task<ActionResult<BaseResponseViewModel<CustomerResponse>>> FindCustomerByPhone(string phoneNumber)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);
                if (customerId == null)
                {
                    return Unauthorized();
                }

                var result = await _customerService.FindCustomer(phoneNumber);
                return Ok(result);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Lấy tất cả order của user 
        /// </summary>
        /// <returns></returns>
        [HttpGet("orders")]
        public async Task<ActionResult<BaseResponsePagingViewModel<OrderResponseForCustomer>>> GetOrderByCustomerId([FromQuery] OrderResponseForCustomer filter,[FromQuery] PagingRequest paging)
        {
            var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);
            if (customerId == null)
            {
                return Unauthorized();
            }
            //var customerId = "4873582B-52AF-4D9E-96D0-0C461018CF81";
            var result = await _orderService.GetOrderByCustomerId(customerId, filter, paging);
            return Ok(result);
        }

        /// <summary>
        /// lấy url VnPay
        /// </summary>
        [HttpGet("topupUrl")]
        public async Task<ActionResult<BaseResponseViewModel<string>>> TopUpWalletRequest(double amount)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);
                if (customerId == null)
                {
                    return Unauthorized();
                }

                var result = await _paymentService.TopUpWalletRequest(customerId, amount);
                return Ok(result);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        ///  Logout
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost("logout")]
        public async Task<ActionResult> Logout([FromBody] LogoutRequest request)
        {
            await _customerService.Logout(request.FcmToken);
            return Ok();
        }

        /// <summary>
        ///  Invitation
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost("invitation")]
        public async Task SendInvitation(string customerId, string partyCode)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var adminId = FireBaseService.GetUserIdFromHeaderToken(accessToken);
                if (adminId == null)
                {
                    Unauthorized();
                }
                //var adminId = "4873582B-52AF-4D9E-96D0-0C461018CF81";
                await _customerService.SendInvitation(customerId, adminId,partyCode);
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

    }
}
