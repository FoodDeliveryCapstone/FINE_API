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

        public CustomerController(ICustomerService customerService, IOrderService orderService)
        {
            _orderService = orderService;
            _customerService = customerService;
        }

        /// <summary>
        /// Google Login
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost("loginByMail")]
        public async Task<ActionResult<CustomerResponse>> LoginByMail([FromBody] ExternalAuthRequest data)
        {
            try
            {
                var result = await _customerService.LoginByMail(data);
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
        /// Lấy tất cả order của user 
        /// </summary>
        /// <returns></returns>
        [HttpGet("orders")]
        public async Task<ActionResult<BaseResponsePagingViewModel<OrderResponse>>> GetOrderByCustomerId([FromQuery] PagingRequest paging)
        {
            var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);
            if (customerId == null)
            {
                return Unauthorized();
            }
            var result = await _orderService.GetOrderByCustomerId(customerId, paging);
            return Ok();
        }

        ///// <summary>
        ///// Update thông tin khách hàng
        ///// </summary>
        ///// <param name="data"></param>
        ///// <returns></returns>
        //[HttpPut("customerId")]
        //public async Task<ActionResult<CustomerResponse>> UpdateCustomer([FromBody] UpdateCustomerRequest data)
        //{
        //    try
        //    {
        //        var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        //        var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);
        //        if (customerId == null)
        //        {
        //            return Unauthorized();
        //        }
        //        var result = await _customerService.UpdateCustomer(customerId, data);
        //        return Ok(result);
        //    }
        //    catch (ErrorResponse ex)
        //    {
        //        return BadRequest(ex.Error);
        //    }
        //}

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

    }
}
