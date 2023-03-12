﻿using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Customer;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiVersion)]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomerController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        /// <summary>
        /// lấy thông tin khách hàng bằng ID
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpGet("customerId")]
        public async Task<ActionResult<CustomerResponse>> GetCustomerById(int customerId)
        {
            try
            {
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
        [HttpPut("customerId")]
        public async Task<ActionResult<CustomerResponse>> UpdateCustomer(int customerId, [FromBody] UpdateCustomerRequest data)
        {
            try
            {
                var result = await _customerService.UpdateCustomer(customerId, data);
                return Ok(result);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }

        /// <summary>
        /// Google Login
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost("login")]
        public async Task<ActionResult<CustomerResponse>> LoginGoogle([FromBody] ExternalAuthRequest data)
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
