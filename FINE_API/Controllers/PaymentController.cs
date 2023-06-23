using FINE.Service.Caches;
using FINE.Service.DTO.Request.Payment;
using FINE.Service.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiVersion)]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }
        //[Cache]
        //[HttpPost("momo")]
        //public async Task<ActionResult> RecieveMomoPayment(MomoIpnRequest request)
        //{
        //    try
        //    {
        //        return null;
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //}
    }
}
