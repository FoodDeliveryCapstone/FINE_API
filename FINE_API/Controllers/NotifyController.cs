using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Area;
using FINE.Service.DTO.Request.Noti;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiVersion)]
    [ApiController]
    public class NotifyController : ControllerBase
    {
        private readonly INotifyService _notifyService;

        public NotifyController(INotifyService notifyService)
        {
            _notifyService = notifyService;
        }

        ///<summary>
        ///Cập nhật các notification đã/chưa đọc
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<BaseResponseViewModel<dynamic>>> UpdateIsReadForNotify([FromQuery] string notifyId, [FromQuery] bool isRead)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (customerId == null)
                {
                    return Unauthorized();
                }
                return Ok(await _notifyService.UpdateIsReadForNotify(notifyId, isRead));
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        ///<summary>
        ///Lấy tất cả notification
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<BaseResponseViewModel<List<NotifyResponse>>>> GetAllNotify()
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (customerId == null)
                {
                    return Unauthorized();
                }
                var result = _notifyService.GetAllNotifyForUser(customerId);

                return Ok(result);
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        ///<summary>
        ///Lấy notification cho user theo ID của notify
        /// </summary>
        [HttpGet("{notifyId}")]
        public async Task<ActionResult<BaseResponseViewModel<NotifyResponse>>> GetNotifyForUserById(string notifyId)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (customerId == null)
                {
                    return Unauthorized();
                }
                return Ok(_notifyService.GetNotifyForUserById(notifyId));
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}
