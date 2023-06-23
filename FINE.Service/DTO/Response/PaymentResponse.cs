using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Response
{
    public class PaymentResponse
    {
        public string OrderId { get; set; }

        public string PayUrl { get; set; }

        public string Deeplink { get; set; }

        public int PaymentType { get; set; }

        public int Status { get; set; }

    }
}
