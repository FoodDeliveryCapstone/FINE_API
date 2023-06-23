using AutoMapper;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.DTO.Request.Payment;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using static FINE.Service.Helpers.Enum;
using SixLabors.Fonts.Unicode;
using System.IdentityModel.Tokens.Jwt;
using Castle.Core.Resource;
using Microsoft.EntityFrameworkCore;
using static FINE.Service.Helpers.ErrorEnum;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;

namespace FINE.Service.Service
{
    public interface IPaymentService
    {
        Task<dynamic> PreparePayment(int paymentType, int type, Order order);
        void RecieveMomoPayment(MomoIpnRequest request);
    }
    public class PaymentService : IPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public PaymentService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
        }

        public async Task<dynamic> PreparePayment(int paymentType, int appType, Order order)
        {
            try
            {
                Payment payment = new Payment();
                if (paymentType == (int)PaymentTypeEnum.MoMo)
                {
                    var redirectURL = "";
                    switch ((PaymentAppTypeEnum)appType)
                    {
                        case PaymentAppTypeEnum.Web:
                            redirectURL = _configuration["Momo:RedirectURL:Web"];

                            break;
                        case PaymentAppTypeEnum.Mobile:
                            redirectURL = _configuration["Momo:RedirectURL:Mobile"];
                            break;
                    }
                    var result = CallMomoService(redirectURL, order);

                    
                }
                else if (paymentType == (int)PaymentTypeEnum.Cash)
                {

                }
                payment.PaymentType = paymentType;
                payment.CreateAt = DateTime.Now;

                return payment;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<bool> CallMomoService(string redirectURL, Order order)
        {
            try
            {
                Guid requestId = Guid.NewGuid();
                var key = $"accessKey={_configuration["Momo:AccessKey"]}"
                        + $"&amount={order.FinalAmount}"
                        + $"&extraData= "
                        + $"&ipnUrl=$ipnUrl"
                        + $"&orderId={order.OrderCode}"
                        + $"&orderInfo={order.OrderCode}"
                        + $"&partnerCode={_configuration["Momo:PartnerCode"]}"
                        + $"&redirectUrl={redirectURL}"
                        + $"&requestId={requestId}"
                        + $"&requestType=captureWallet";
                Encoding ascii = Encoding.ASCII;
                HMACSHA256 hmac = new HMACSHA256();
                String signature = Convert.ToBase64String(hmac.ComputeHash(ascii.GetBytes(key)));

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(_configuration["Momo:Url"]);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var momoPayment = new MomoRequestModel()
                    {
                        PartnerCode = _configuration["Momo:PartnerCode"],
                        StoreName = "FINE",
                        RequestType = "captureWallet",
                        IpnUrl = "", //api nhận kq
                        RedirectUrl = redirectURL,
                        OrderId = order.OrderCode,
                        RequestId = requestId.ToString(),
                        OrderInFo = order.OrderCode,
                        Amount = (long)order.FinalAmount,
                        Lang = "vi",
                        UserInfo = new UserInfo()
                        {
                            Name = order.Customer.Name,
                            PhoneNumber = order.Customer.Phone,
                            Email = order.Customer.Email
                        },
                        Signature = signature
                    };
                    HttpResponseMessage response = await client.PostAsJsonAsync<MomoRequestModel>("api/create", momoPayment);
                    if(!response.IsSuccessStatusCode)
                        throw new ErrorResponse(400, (int)PaymentErrorEnums.PAYMENT_FAIL,
                                                       PaymentErrorEnums.PAYMENT_FAIL.GetDisplayName());

                    var result = await response.Content.ReadAsAsync<MomoResultModel>();
                    if(result.ResultCode == 0)
                    {

                    }
                    else // nếu fail thì tìm cách xử lý :>>>
                    {

                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async void RecieveMomoPayment(MomoIpnRequest request)
        {
            try
            {
                if (request.ResultCode == (int)MomoResultCode.Success)
                {
                    var payment = await _unitOfWork.Repository<Payment>().GetAll()
                        .Include(x => x.Order)
                        .FirstOrDefaultAsync(x => x.Order.OrderCode == request.OrderInfo);

                    payment.Status = (int)PaymentStatusEnum.Finish;
                    payment.Order.OrderStatus = (int)OrderStatusEnum.Processing;

                }
            }catch (Exception ex)
            {

            }
        }
    }
}
