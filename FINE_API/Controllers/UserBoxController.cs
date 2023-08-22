using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using IronSoftware.Drawing;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiVersion)]
    [ApiController]
    public class UserBoxController : Controller
    {
        private readonly IQrCodeService _qrCodeService;

        public UserBoxController(IQrCodeService qrCodeService)
        {
            _qrCodeService = qrCodeService;
        }

        /// <summary>
        /// Get order by Id
        /// </summary>
        [HttpGet("qrCode")]
        public IActionResult GetQRCode(string boxId)
        {
            var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

            if (customerId == null)
            {
                return Unauthorized();
            }
            //var customerId = "3D596DBF-E43E-45E6-85DD-50CD1095E362";

            var qrCode = _qrCodeService.GenerateQrCode(customerId, boxId).Result;
            byte[] qrCodeBytes;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                qrCode.SaveAsPng(memoryStream.ToString());
                qrCodeBytes = memoryStream.ToArray();
            }

            return File(qrCodeBytes, "image/png");
        }
    }
}
