﻿using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
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
        private readonly IConfiguration _configuration;
        public UserBoxController(IQrCodeService qrCodeService, IConfiguration configuration)
        {
            _qrCodeService = qrCodeService;
            _configuration = configuration;
        }

        /// <summary>
        /// Get user qrcode
        /// </summary>
        [HttpGet("qrCode")]
        public IActionResult GetQRCode(string orderId)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var customerId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (customerId == null)
                {
                    return Unauthorized();
                }
                //var customerId = "CD59782C-998C-4693-9920-F1FE4964C24A";

                var qrCodeBitmap = _qrCodeService.GenerateQrCode(customerId, orderId).Result;
                using (MemoryStream stream = new MemoryStream())
                {
                    qrCodeBitmap.Save(stream, ImageFormat.Png);
                    byte[] imageBytes = stream.ToArray();

                    return File(imageBytes, "image/png");
                }
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Get shipper qrcode
        /// </summary>
        [HttpGet("qrCodeShipper")]
        public IActionResult GetQRCodeShipper(string timeSlotId)
        {
            try
            {
                var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var staffId = FireBaseService.GetUserIdFromHeaderToken(accessToken);

                if (staffId == null)
                {
                    return Unauthorized();
                }

                var qrCodeBitmap = _qrCodeService.GenerateShipperQrCode(staffId, timeSlotId).Result;
                using (MemoryStream stream = new MemoryStream())
                {
                    qrCodeBitmap.Save(stream, ImageFormat.Png);
                    byte[] imageBytes = stream.ToArray();

                    return File(imageBytes, "image/png");
                }
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Receive Box Result
        /// </summary>
        [HttpPost("return")]
        public async Task<ActionResult<BaseResponseViewModel<dynamic>>> ReceiveBoxResult(string orderId, string iotCode)
        {
            try
            {
                if (!iotCode.Equals(_configuration["IOTCode"]))
                {
                    return Unauthorized();
                }
                return await _qrCodeService.ReceiveBoxResult(orderId);
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Get box and key for IOT system
        /// </summary>
        [HttpGet("boxKey")]
        public async Task<ActionResult<BaseResponseViewModel<QROrderBoxResponse>>> GetListBoxAndKey(string staffId, string timeslotId, string iotCode)
        {
            try
            {
                if(!iotCode.Contains(_configuration["IOTCode"]))
                {
                    return Unauthorized() ;
                }
                return await _qrCodeService.GetListBoxAndKey(staffId, timeslotId);
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}
