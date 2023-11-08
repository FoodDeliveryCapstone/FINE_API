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
        Task<bool> PaymentExecute(IQueryCollection collections);
        Task<BaseResponseViewModel<dynamic>> RefundPartialLinkedFee(string partyCode, Guid customerId);
        Task<BaseResponseViewModel<dynamic>> RefundRefuseAmount(List<OtherAmount> otherAmount);
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
                var note = $"Thanh toán {order.FinalAmount} VND cho đơn hàng {order.OrderCode}";
                await _accountService.CreateTransaction(TransactionTypeEnum.Payment, AccountTypeEnum.CreditAccount, order.FinalAmount, order.CustomerId, TransactionStatusEnum.Finish, note);

                var payment = new Payment()
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    Amount = order.FinalAmount,
                    PaymentType = (int)paymentType,
                    //Status = (int)PaymentStatusEnum.Finish,
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
                var orderInfo = $"Nap {amount} VND vao tai khoan he thong FINE";
                var transaction = await _accountService.CreateTransaction(TransactionTypeEnum.Recharge, AccountTypeEnum.CreditAccount, amount, Guid.Parse(customerId), TransactionStatusEnum.Processing, orderInfo);

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
                pay.AddRequestData("vnp_TxnRef", transaction.Id.ToString());

                var paymentUrl = pay.CreateRequestUrl(_configuration["VnPayment:BaseUrl"], _configuration["VnPayment:SecureHash"]);

                await _unitOfWork.CommitAsync();
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

        public async Task<bool> PaymentExecute(IQueryCollection collections)
        {
            try
            {
                bool isSuccess;
                var pay = new VnPayLibrary();
                var response = pay.GetFullResponseData(collections, _configuration["VnPayment:SecureHash"]);
                var transaction = await _unitOfWork.Repository<Transaction>().GetAll()
                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(response.TransactionId));

                if (response.Success is true && response.VnPayResponseCode.Equals("00"))
                {
                    transaction.Status = (int)TransactionStatusEnum.Finish;
                    transaction.Account.Balance += transaction.Amount;
                    isSuccess = true;
                }
                else
                {
                    transaction.Status = (int)TransactionStatusEnum.Fail;
                    isSuccess = false;
                }
                transaction.UpdatedAt = DateTime.Now;
                await _unitOfWork.Repository<Transaction>().UpdateDetached(transaction);
                await _unitOfWork.CommitAsync();
                return isSuccess;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<dynamic>> RefundPartialLinkedFee(string partyCode, Guid customerId)
        {
            try
            {
                var parties = _unitOfWork.Repository<Party>().GetAll().Where(x => x.PartyCode == partyCode).ToList();
                if (parties.Count() == 2)
                {
                    if (parties.Where(x => x.Order.OrderStatus == (int)OrderStatusEnum.Finished).Count() > 1)
                    {
                        foreach (var party in parties)
                        {
                            var customerFcm = _unitOfWork.Repository<Fcmtoken>().GetAll().FirstOrDefault(x => x.UserId == party.CustomerId);
                            var shippingFee = party.Order.OtherAmounts.FirstOrDefault(x => x.Type == (int)OtherAmountTypeEnum.ShippingFee).Amount;

                            var discountRate = Int32.Parse(_configuration["LinkedDiscountRate"]);
                            var refundFee = shippingFee * discountRate;

                            party.Status = (int)PartyOrderStatus.FinishRefund;
                            party.UpdateAt = DateTime.Now;
                            _unitOfWork.Repository<Party>().UpdateDetached(party);

                            var note = $"Hoàn phí áp dụng mã liên kết {party.PartyCode}: {refundFee} VND";
                            _accountService.CreateTransaction(TransactionTypeEnum.Recharge, AccountTypeEnum.CreditAccount, refundFee, party.CustomerId, TransactionStatusEnum.Finish, note);

                            Notification notification = new Notification()
                            {
                                Title = "Thông báo!!!",
                                Body = note
                            };
                            Dictionary<string, string> data = new Dictionary<string, string>()
                            {
                                { "type", NotifyTypeEnum.ForPopup.ToString()}
                            };
                            BackgroundJob.Enqueue(() => _fm.SendToToken(customerFcm.Token, notification, data));
                        }
                    }
                }
                else if (parties.Count() > 2)
                {
                    var party = parties.FirstOrDefault(x => x.CustomerId == customerId);
                    var customerFcm = _unitOfWork.Repository<Fcmtoken>().GetAll().FirstOrDefault(x => x.UserId == party.CustomerId);
                    var shippingFee = party.Order.OtherAmounts.FirstOrDefault(x => x.Type == (int)OtherAmountTypeEnum.ShippingFee).Amount;
                    var refundFee = shippingFee * (Int32.Parse(_configuration["LinkedDiscountRate"]) / 100);

                    party.Status = (int)PartyOrderStatus.FinishRefund;
                    party.UpdateAt = DateTime.Now;
                    _unitOfWork.Repository<Party>().UpdateDetached(party);

                    var note = $"Hoàn phí áp dụng mã liên kết {party.PartyCode}: {refundFee} VND";
                    _accountService.CreateTransaction(TransactionTypeEnum.Recharge, AccountTypeEnum.CreditAccount, refundFee, party.CustomerId, TransactionStatusEnum.Finish, note);

                    Notification notification = new Notification()
                    {
                        Title = "Thông báo!!!",
                        Body = note
                    };
                    Dictionary<string, string> data = new Dictionary<string, string>()
                        {
                            { "type", NotifyTypeEnum.ForRefund.ToString()}
                        };
                    BackgroundJob.Enqueue(() => _fm.SendToToken(customerFcm.Token, notification, data));
                }
                return new BaseResponseViewModel<dynamic>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    }
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<dynamic>> RefundRefuseAmount(List<OtherAmount> otherAmount)
        {
            try
            {
                var amount = otherAmount.Select(x => x.Amount).Sum();
                var order = otherAmount.FirstOrDefault().Order;
                var note = $"Hoàn {amount} VND cho đơn hàng {order.OrderCode}";
                _accountService.CreateTransaction(TransactionTypeEnum.Recharge, AccountTypeEnum.CreditAccount,amount, order.CustomerId, TransactionStatusEnum.Finish, note);
                var customerFcm = _unitOfWork.Repository<Fcmtoken>().GetAll().FirstOrDefault(x => x.UserId == order.CustomerId);
                Notification notification = new Notification()
                {
                    Title = "Thông báo!!!",
                    Body = note
                };
                Dictionary<string, string> data = new Dictionary<string, string>()
                {
                    { "type", NotifyTypeEnum.ForRefund.ToString()}
                };

                BackgroundJob.Enqueue(() => _fm.SendToToken(customerFcm.Token, notification, data));

                return new BaseResponseViewModel<dynamic>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    }
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}
