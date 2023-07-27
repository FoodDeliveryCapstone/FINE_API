//using FINE.Service.DTO.Request;
//using FINE.Service.DTO.Response;
//using FINE.Service.Exceptions;
//using FINE.Service.Service;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;

//namespace FINE.API.Controllers
//{
//    [Route(Helpers.SettingVersionApi.ApiAdminVersion + "/customer")]
//    [ApiController]
//    public class AdminCustomerController : ControllerBase
//    {
//        private readonly ICustomerService _customerService;

//        public AdminCustomerController(ICustomerService customerService)
//        {
//            _customerService = customerService;
//        }

//        /// <summary>
//        /// Get list Customer for system admin
//        /// </summary>
//        [Authorize(Roles = "SystemAdmin")]
//        [HttpGet()]
//        public async Task<ActionResult<BaseResponsePagingViewModel<CustomerResponse>>> GetCustomers([FromRoute] CustomerResponse customerResponse, [FromRoute] PagingRequest pagingRequest)
//        {
//            return await _customerService.GetCustomers(customerResponse, pagingRequest);
//        }
   
       
//    }
//}
