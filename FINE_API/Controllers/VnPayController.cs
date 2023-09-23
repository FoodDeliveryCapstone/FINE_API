using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers
{
    public class VnPayController : Controller
    {
        private readonly IPaymentService _paymentService;

        public VnPayController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        /////<summary>
        /////lấy kết quả trả về từ VnPay
        ///// </summary>
        //[HttpPost("ReturnUrl")]
        //public async Task<IActionResult> PaymentExecute([FromQuery] string? Vnp_Amount, [FromQuery] string? Vnp_BankCode,
        //    [FromQuery] string? Vnp_BankTranNo, [FromQuery] string? Vnp_CardType, [FromQuery] string? Vnp_OrderInfo, [FromQuery] string? Vnp_PayDate,
        //    [FromQuery] string? Vnp_ResponseCode, [FromQuery] string? Vnp_TmnCode, [FromQuery] string? Vnp_TransactionNo, [FromQuery] string? Vnp_TxnRef,
        //    [FromQuery] string? Vnp_SecureHashType, [FromQuery] string? Vnp_SecureHash)
        //{
        //    try
        //    {
        //        IQueryCollection url = HttpContext.Request.Query;

        //        await _paymentService.PaymentExecute(url);

        //        return RedirectPermanent()
        //    } 
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}
    }
}
