﻿using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Mvc;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Http.Headers;

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
            //var customerId = "CD59782C-998C-4693-9920-F1FE4964C24A";

            var qrCodeBitmap = _qrCodeService.GenerateQrCode(customerId, boxId).Result;
            using (MemoryStream stream = new MemoryStream())
            {
                qrCodeBitmap.Save(stream, ImageFormat.Png);
                byte[] imageBytes = stream.ToArray();

                return File(imageBytes, "image/png");
            }
        }
    }
}