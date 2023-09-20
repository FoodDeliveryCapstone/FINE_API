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
        [HttpPost("ReturnUrl")]
        public async Task PaymentExecute([FromQuery] string? Vnp_Amount, [FromQuery] string? Vnp_BankCode,
            [FromQuery] string? Vnp_BankTranNo, [FromQuery] string? Vnp_CardType, [FromQuery] string? Vnp_OrderInfo, [FromQuery] string? Vnp_PayDate,
            [FromQuery] string? Vnp_ResponseCode, [FromQuery] string? Vnp_TmnCode, [FromQuery] string? Vnp_TransactionNo, [FromQuery] string? Vnp_TxnRef,
            [FromQuery] string? Vnp_SecureHashType, [FromQuery] string? Vnp_SecureHash)
        {
            try
            {
                IQueryCollection url = HttpContext.Request.Query;

                await _paymentService.PaymentExecute(url);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
//https://localhost:44362/ReturnUrl?
// vnp_Amount=1000000
// &vnp_BankCode=NCB
// &vnp_BankTranNo=20170829152730
// &vnp_CardType=ATM
// &vnp_OrderInfo=Thanh+toan+don+hang+thoi+gian%3A+2017-08-29+15%3A27%3A02
// &vnp_PayDate=20170829153052
// &vnp_ResponseCode=00
// &vnp_TmnCode=2QXUI4J4
// &vnp_TransactionNo=12996460
// &vnp_TxnRef=23597
// &vnp_SecureHashType=SHA256
// &vnp_SecureHash=20081f0ee1cc6b524e273b6d4050fefd
