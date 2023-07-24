//using System.Net.NetworkInformation;
//using FINE.Service.DTO.Request;
//using FINE.Service.DTO.Request.Order;
//using FINE.Service.DTO.Response;
//using FINE.Service.Exceptions;
//using FINE.Service.Service;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;


//namespace FINE.API.Controllers.AdminControler
//{
//    [Route(Helpers.SettingVersionApi.ApiAdminVersion + "/order")]
//    [ApiController]
//    public class AdminOrderController : ControllerBase
//    {
//        private readonly IOrderService _orderService;

//        public AdminOrderController(IOrderService orderService)
//        {
//            _orderService = orderService;
//        }

//        /// <summary>
//        /// Get orders for staff
//        /// </summary>
//        [Authorize(Roles = "StoreManager")]
//        [HttpGet("storeManager")]
//        public async Task<ActionResult<BaseResponsePagingViewModel<GenOrderResponse>>> GetOrdersForStaff([FromQuery] PagingRequest paging)
//        {
//            try
//            {
//                return await _orderService.GetOrders(paging);
//            }
//            catch (ErrorResponse ex)
//            {
//                return BadRequest(ex.Error);
//            }
//        }
//    }
//}
