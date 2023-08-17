using AutoMapper;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
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
        Task CreatePayment(Order orderId, int point, int type);
    }
    public class PaymentService : IPaymentService
    {
        private readonly IAccountService _accountService;
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public PaymentService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, IAccountService accountService)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _accountService = accountService;
        }

        public async Task CreatePayment(Order order, int point, int paymentType)
        {
            try
            {
                await _accountService.CreateTransaction(TransactionTypeEnum.Payment, AccountTypeEnum.CreditAccount, order.FinalAmount, order.CustomerId);

                var payment = new Payment()
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    Amount = order.FinalAmount,
                    PaymentType = paymentType,
                    Status = (int)PaymentStatusEnum.Finish,
                    CreateAt = DateTime.Now
                };

                await _unitOfWork.Repository<Payment>().InsertAsync(payment);
               
                await _accountService.CreateTransaction(TransactionTypeEnum.Recharge, AccountTypeEnum.PointAccount, point, order.CustomerId);
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}
