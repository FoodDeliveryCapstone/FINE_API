using FINE.Service.DTO.Request;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiAdminVersion + "/customer")]
    [ApiController]
    public class AdminCustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public AdminCustomerController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        /// <summary>
        /// Get Customer by Id
        /// </summary>
        [Authorize(Roles = "SystemAdmin")]
        [HttpGet("{customerId}")]
        public async Task<ActionResult<BaseResponseViewModel<CustomerResponse>>> GetCustomerById([FromRoute] string customerId)
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
        /// Get Customers
        /// </summary>
        [Authorize(Roles = "SystemAdmin")]
        [HttpGet]
        public async Task<ActionResult<BaseResponsePagingViewModel<CustomerResponse>>> GetCustomers([FromQuery] CustomerResponse filter, [FromQuery] PagingRequest paging)
        {
            try
            {
                var result = await _customerService.GetCustomers(filter, paging);
                return Ok(result);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }


    }
}
