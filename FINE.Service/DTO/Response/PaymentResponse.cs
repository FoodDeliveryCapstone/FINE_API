using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class PaymentResponse
    {
        //OrderInfo
        public string OrderDescription { get; set; }
        //Mã giao dịch ghi nhận tại hệ thống VNPAY.
        public string VnPayTransactionId { get; set; }
        public string TransactionId { get; set; }
        public string PaymentMethod { get; set; }
        public bool Success { get; set; }
        public string Token { get; set; }
        public string VnPayResponseCode { get; set; }
    }
}
