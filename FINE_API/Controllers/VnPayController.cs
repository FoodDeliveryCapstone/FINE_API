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

        ///<summary>
        ///lấy kết quả trả về từ VnPay
        /// </summary>
        [HttpGet("ReturnUrl")]
        public async Task<IActionResult> PaymentExecute([FromQuery] string? Vnp_Amount, [FromQuery] string? Vnp_BankCode,
            [FromQuery] string? Vnp_BankTranNo, [FromQuery] string? Vnp_CardType, [FromQuery] string? Vnp_OrderInfo, [FromQuery] string? Vnp_PayDate,
            [FromQuery] string? Vnp_ResponseCode, [FromQuery] string? Vnp_TmnCode, [FromQuery] string? Vnp_TransactionNo, [FromQuery] string? Vnp_TxnRef,
            [FromQuery] string? Vnp_SecureHashType, [FromQuery] string? Vnp_SecureHash)
        {
            try
            {
                IQueryCollection url = HttpContext.Request.Query;

                bool isSuccessful =  await _paymentService.PaymentExecute(url);

                if (isSuccessful is true)
                {
                    return RedirectPermanent("https://firebasestorage.googleapis.com/v0/b/fine-mobile-21acd.appspot.com/o/images%2Fpayment-done.png?alt=media&token=c22ca308-9711-4ecf-afef-f79a60594acb");
                }
                else
                {
                    return RedirectPermanent("https://firebasestorage.googleapis.com/v0/b/fine-mobile-21acd.appspot.com/o/images%2Fpayment-fail.png?alt=media&token=f9be174d-d5f4-4c67-8310-e8ff8bb6ee2b");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
