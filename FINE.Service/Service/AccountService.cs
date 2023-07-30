using AutoMapper;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IAccountService
    {
        Task<BaseResponseViewModel<bool>> CreateTransaction(int accountType, double amount, Guid customerId);
        void CreateAccount(Guid customerId, int accountType);
    }

    public class AccountService : IAccountService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public AccountService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async void CreateAccount(Guid customerId, int accountType)
        {
            try
            {
                var newAccount = new Account()
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    AccountCode = customerId + "_" + accountType.ToString(),
                    StartDate = DateTime.Now,
                    Balance = 0,
                    Type = accountType,
                    IsActive = true,
                    CreateAt = DateTime.Now
                };

                await _unitOfWork.Repository<Account>().InsertAsync(newAccount);
                await _unitOfWork.CommitAsync();
                
            }
            catch (Exception ex)
            {
            }
        }

        public async Task<BaseResponseViewModel<bool>> CreateTransaction(int accountType, double amount, Guid customerId)
        {
            try
            {
                var account = await _unitOfWork.Repository<Account>().GetAll()
                                .Where(x => x.CustomerId == customerId)
                                .ToListAsync();

                if (accountType == (int)AccountTypeEnum.CreditAccount)
                {
                    var creditAccount = account.FirstOrDefault(x => x.Type == (int)AccountTypeEnum.CreditAccount);
                    //check balance customer xem đủ ko 
                    if (creditAccount.Balance < amount)
                        throw new ErrorResponse(400, (int)PaymentErrorsEnum.ERROR_BALANCE,
                            PaymentErrorsEnum.ERROR_BALANCE.GetDisplayName());
                    creditAccount.Balance -= amount;
                    creditAccount.UpdateAt = DateTime.Now;

                    try
                    {
                        var transaction = new Transaction()
                        {
                            Id = Guid.NewGuid(),
                            AccountId = creditAccount.Id,
                            Amount = amount,
                            IsIncrease = false,
                            Status = (int)TransactionStatusEnum.Finish,
                            Type = (int)TransactionTypeEnum.Payment,
                            CreatedAt = DateTime.Now
                        };

                        await _unitOfWork.Repository<Transaction>().InsertAsync(transaction);
                        await _unitOfWork.Repository<Account>().UpdateDetached(creditAccount);
                        await _unitOfWork.CommitAsync();

                    }
                    catch (ErrorResponse ex)
                    {
                        throw new ErrorResponse(400, (int)TransactionErrorEnum.CREATE_TRANS_FAIL,
                            TransactionErrorEnum.CREATE_TRANS_FAIL.GetDisplayName());
                    }
                }
                else if (accountType == (int)AccountTypeEnum.PointAccount)
                {
                    var pointAccount = account.FirstOrDefault(x => x.Type == (int)AccountTypeEnum.PointAccount);
                    pointAccount.Balance += amount;
                    pointAccount.UpdateAt = DateTime.Now;
                    await _unitOfWork.Repository<Account>().UpdateDetached(pointAccount);
                    await _unitOfWork.CommitAsync();
                }

                return new BaseResponseViewModel<bool>
                {
                    Status = new StatusViewModel
                    {
                        Success = true,
                        Message = "Success",
                        ErrorCode = 0
                    }
                };
            }
            catch (ErrorResponse ex)
            {
                return new BaseResponseViewModel<bool>
                {
                    Status = new StatusViewModel
                    {
                        Success = false,
                        Message = ex.Error.Message,
                        ErrorCode = ex.Error.ErrorCode
                    }
                };
            }
        }
    }
}

