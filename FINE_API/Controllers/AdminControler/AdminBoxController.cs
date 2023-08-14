using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Box;
using FINE.Service.DTO.Request.Destination;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiAdminVersion + "/box")]
    [ApiController]
    public class AdminBoxController : ControllerBase
    {
        private readonly IBoxService _boxService;

        public AdminBoxController(IBoxService boxService)
        {
            _boxService = boxService;
        }

        /// <summary>
        /// Check Box code
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<BaseResponseViewModel<BoxResponse>>> CheckBoxCode([FromBody] CheckBoxCodeRequest request)
        {
            try
            {
                return await _boxService.CheckBoxCode(request);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }
    }
}
