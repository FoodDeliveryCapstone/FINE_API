using FINE.Service.DTO.Request.Order;
using FINE.Service.DTO.Response;
using FINE.Service.Service;
using Microsoft.AspNetCore.Mvc;
using static FINE.Service.Helpers.Enum;

namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiVersion)]
    [ApiController]
    public class LogController : Controller
    {
        private readonly ILogService _logService;
        public LogController(ILogService logService)
        {
            _logService = logService;
        }

        /// <summary>
        /// Catch Log
        /// </summary>
        [HttpPost]
        public async void CatchLog(AppCatchLog appCatch, [FromBody]string mess)
        {
            try
            {
                _logService.CatchLog(appCatch, mess);
            }
            catch (Exception ex)
            {
                BadRequest(ex);
            }
        }
    }
}
