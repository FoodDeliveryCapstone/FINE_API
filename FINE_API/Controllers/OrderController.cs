using FINE.Service.DTO.Request.Order;
using FINE.Service.DTO.Response;
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
        /// Create PreOrder
        /// </summary>
        [HttpPost("preOrder")]
        public async Task<ActionResult<BaseResponseViewModel<GenOrderResponse>>> CreatePreOrder([FromBody] CreateOrderRequest request)
        {
                return await _orderService.CreatePreOrder(request);        
        }
    }
}
