//using FINE.Service.DTO.Request;
//using FINE.Service.DTO.Request.Area;
//using FINE.Service.DTO.Request.Noti;
//using FINE.Service.DTO.Response;
//using FINE.Service.Service;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;

//namespace FINE.API.Controllers
//{
//    [Route(Helpers.SettingVersionApi.ApiVersion)]
//    [ApiController]
//    public class NotifyController : ControllerBase
//    {
//        private readonly INotifyService _notifyService;

//        public NotifyController(INotifyService notifyService)
//        {
//            _notifyService = notifyService;
//        }

//        /// <summary>
//        /// Get List notifys    
//        /// </summary>
//        /// 
//        [HttpGet]
//        public async Task<ActionResult<BaseResponsePagingViewModel<NotifyResponse>>> GetNotifys
//            ([FromQuery] NotifyResponse request, [FromQuery] PagingRequest paging)
//        {
//            return await _notifyService.GetNotifys(request, paging);
//        }

//        /// <summary>
//        /// Get Notify By Id
//        /// </summary>
//        /// 
//        [HttpGet("{notifyId}")]
//        public async Task<ActionResult<BaseResponseViewModel<NotifyResponse>>> GetNotifyById
//            ([FromRoute] int notifyId)
//        {
//            return await _notifyService.GetNotifyById(notifyId);
//        }

//        /// <summary>
//        /// Create NOtify                        
//        /// </summary>
//        /// 
//        [HttpPost]
//        public async Task<ActionResult<bool>> CreateNotify
//            ([FromBody] NotifyRequestModel request)
//        {
//            return await _notifyService.CreateOrderNotify(request);
//        }

//        /// <summary>
//        /// Update Notify
//        /// </summary>
//        /// 
//        [HttpPut("{notifyId}")]
//        public async Task<ActionResult<BaseResponseViewModel<NotifyResponse>>> UpdateNotify
//            ([FromRoute] int notifyId, [FromBody] UpdateNotifyRequest request)
//        {
//            return await _notifyService.UpdateNotify(notifyId, request);
//        }
//    }
//}
