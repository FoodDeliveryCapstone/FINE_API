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
        public string StoreName { get; set; }
        public string RequestType { get; set; } 
        public string IpnUrl { get; set; }
        public string RedirectUrl { get; set;}
        public string OrderId { get; set; }
        public string RequestId { get; set; }
        public string OrderInFo { get; set; }
        public long Amount { get; set; }
        public string Lang { get; set; }
        public UserInfo UserInfo { get; set; }
        public string Signature { get; set; }
    }
    public class UserInfo
    {
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
    }
    public class MomoResultModel
    {
        public string PartnerCode { get; set; }
        public string RequestId { get; set; }
        public string OrderId { get; set; }
        public long Amount { get; set; }
        public long ResponseTime { get; set; }
        public string Message { get; set; }
        public int ResultCode { get; set; }
        public string PayUrl { get; set; }
        public string Deeplink { get; set; }
        public string Signature { get; set; }

    }
    public class MomoIpnRequest
    {
        public string PartnerCode { get; set; }
        public string RequestId { get; set; }
        public string OrderId { get; set; }
        public long Amount { get; set; }
        public string OrderInfo { get; set; }
        public string OrderType { get; set; }
        public long TransId { get; set; }
        public int ResultCode { get; set; }
        public string Message { get; set; }
        public string PayType { get; set; }
        public string Signature { get; set; }
    }
}
