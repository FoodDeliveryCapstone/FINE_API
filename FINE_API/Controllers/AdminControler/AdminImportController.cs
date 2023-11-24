using FINE.Service.DTO.Request.ProductInMenu;
using FINE.Service.DTO.Response;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers.AdminControler
{
    [Route(Helpers.SettingVersionApi.ApiAdminVersion + "/import")]
    [ApiController]
    public class AdminImportController : Controller
    {
        private readonly IImportService _importService;

        public AdminImportController(IImportService importService)
        {
            _importService = importService;
        }

        /// <summary>
        /// Import Product
        /// </summary>    
        [Authorize(Roles = "SystemAdmin")]
        [HttpPost]
        public async Task<ActionResult<BaseResponseViewModel<ImportResponse>>> ImportProduct([FromForm] IFormFile excelFile)
        {
            try
            {
                return await _importService.ImportProductsByExcel(excelFile);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
