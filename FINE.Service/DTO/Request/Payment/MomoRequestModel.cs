using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.DTO.Request.Payment
{
    public class MomoRequestModel
    {
        public string PartnerCode { get; set; }
        public string PartnerName { get; set; }
        public string RequestType { get; set; } 
        public string IpnUrl { get; set; }
        public string RedirectUrl { get; set;}
        public string OrderId { get; set; }
        public double Amount { get; set; }
        public string Lang { get; set; }

    }
}
