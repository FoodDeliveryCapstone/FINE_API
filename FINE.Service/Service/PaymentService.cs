using AutoMapper;
using Castle.Core.Resource;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Attributes;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Helpers;
using FINE.Service.Utilities;
using FirebaseAdmin.Messaging;
using Hangfire;
using Hangfire.MemoryStorage.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IPaymentService
    {
        Task CreatePayment(Order orderId, int point, PaymentTypeEnum type);
        Task<BaseResponseViewModel<string>> TopUpWalletRequest(string customerId, double amount);
        Task PaymentExecute(IQueryCollection collections);
    }
    public class PaymentService : IPaymentService
    {
        private readonly IAccountService _accountService;
        private readonly IFirebaseMessagingService _fm;
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public PaymentService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, IAccountService accountService, IFirebaseMessagingService fm)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _accountService = accountService;
            _fm = fm;
        }

        public async Task CreatePayment(Order order, int point, PaymentTypeEnum paymentType)
        {
            try
            {
                await _accountService.CreateTransaction(TransactionTypeEnum.Payment, AccountTypeEnum.CreditAccount, order.FinalAmount, order.CustomerId, TransactionStatusEnum.Finish);

                var payment = new Payment()
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    Amount = order.FinalAmount,
                    PaymentType = (int)paymentType,
                    Status = (int)PaymentStatusEnum.Finish,
                    CreateAt = DateTime.Now
                };

                await _unitOfWork.Repository<Payment>().InsertAsync(payment);

                await _accountService.CreateTransaction(TransactionTypeEnum.Recharge, AccountTypeEnum.PointAccount, point, order.CustomerId, TransactionStatusEnum.Finish);
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<string>> TopUpWalletRequest(string customerId, double amount)
        {
            try
            {
                var account = await _unitOfWork.Repository<Account>().GetAll()
                    .FirstOrDefaultAsync(x => x.Customer.Id == Guid.Parse(customerId)
                                        && x.Type == (int)AccountTypeEnum.CreditAccount);
                var orderInfo = $"Nap {amount} vao tai khoan he thong FINE";
                await _accountService.CreateTransaction(TransactionTypeEnum.Recharge, AccountTypeEnum.CreditAccount, amount, Guid.Parse(customerId), TransactionStatusEnum.Processing, orderInfo);

                var transactionId = Guid.NewGuid();
                var pay = new VnPayLibrary();
                var urlCallBack = _configuration["VnPayment:ReturnUrl"];

                pay.AddRequestData("vnp_Version", _configuration["VnPayment:Version"]);
                pay.AddRequestData("vnp_Command", _configuration["VnPayment:Command"]);
                pay.AddRequestData("vnp_TmnCode", _configuration["VnPayment:TmnCode"]);
                pay.AddRequestData("vnp_Amount", ((int)amount * 100).ToString());
                pay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
                pay.AddRequestData("vnp_CurrCode", _configuration["VnPayment:CurrCode"]);
                pay.AddRequestData("vnp_IpAddr", "52.221.192.64");
                pay.AddRequestData("vnp_Locale", _configuration["VnPayment:Locale"]);
                pay.AddRequestData("vnp_OrderInfo", orderInfo.ToString());
                pay.AddRequestData("vnp_OrderType", _configuration["VnPayment:OrderType"]);
                pay.AddRequestData("vnp_ReturnUrl", urlCallBack);
                pay.AddRequestData("vnp_TxnRef", transactionId.ToString());

                var paymentUrl = pay.CreateRequestUrl(_configuration["VnPayment:BaseUrl"], _configuration["VnPayment:SecureHash"]);

                return new BaseResponseViewModel<string>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = paymentUrl
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task PaymentExecute(IQueryCollection collections)
        {
            try
            {
                var pay = new VnPayLibrary();
                var response = pay.GetFullResponseData(collections, _configuration["VnPayment:SecureHash"]);
                var transaction = await _unitOfWork.Repository<Transaction>().GetAll()
                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(response.TransactionId));

                var customerToken = _unitOfWork.Repository<Fcmtoken>().GetAll().FirstOrDefault(x => x.UserId == transaction.Account.Customer.Id).Token;
                Notification notification = null;
                var data = new Dictionary<string, string>()
                {
                    { "type", NotifyTypeEnum.ForPopup.ToString()}
                };

                if (response.Success == false)
                {
                    notification = new Notification
                    {
                        Title = Constants.VNPAY_PAYMENT_FAIL,
                        Body = Constants.VNPAY_PAYMENT_FAIL
                    };
                }
                else if (response.VnPayResponseCode == "00")
                {
                    transaction.Status = (int)TransactionStatusEnum.Finish;
                    transaction.UpdatedAt = DateTime.Now;

                    await _unitOfWork.Repository<Transaction>().UpdateDetached(transaction);
                    await _unitOfWork.CommitAsync();

                    notification = new Notification
                    {
                        Title = Constants.VNPAY_PAYMENT_SUCC,
                        Body = Constants.VNPAY_PAYMENT_SUCC
                    };
                }
                else
                {
                    notification = new Notification
                    {
                        Title = Constants.VNPAY_PAYMENT_FAIL,
                        Body = Constants.VNPAY_PAYMENT_FAIL
                    };
                }

                BackgroundJob.Enqueue(() => _fm.SendToToken(customerToken, notification, data));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
