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
        Task CreateTransaction(TransactionTypeEnum transactionTypeEnum, AccountTypeEnum accountType, double amount, Guid customerId);
        void CreateAccount(Guid customerId);
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

        public async void CreateAccount(Guid customerId)
        {
            try
            {
                var newPointAccount = new Account()
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    AccountCode = customerId + "_" + AccountTypeEnum.PointAccount,
                    StartDate = DateTime.Now,
                    Balance = 0,
                    Type = (int)AccountTypeEnum.PointAccount,
                    IsActive = true,
                    CreateAt = DateTime.Now
                };
                await _unitOfWork.Repository<Account>().InsertAsync(newPointAccount);

                var newCreditAccount = new Account()
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    AccountCode = customerId + "_" + AccountTypeEnum.CreditAccount,
                    StartDate = DateTime.Now,
                    Balance = 10000,
                    Type = (int)AccountTypeEnum.CreditAccount,
                    IsActive = true,
                    CreateAt = DateTime.Now,
                    Transactions = new List<Transaction>()
                };

                var transaction = new Transaction()
                {
                    Id = Guid.NewGuid(),
                    AccountId = newCreditAccount.Id,
                    Amount = 10000,
                    IsIncrease = true,
                    Status = (int)TransactionStatusEnum.Finish,
                    Type = (int)TransactionTypeEnum.Recharge,
                    Notes = "chào đón khách hàng mới",
                    CreatedAt = DateTime.Now
                };
                newCreditAccount.Transactions.Add(transaction);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task CreateTransaction(TransactionTypeEnum transactionType, AccountTypeEnum accountType, double amount, Guid customerId)
        {
            try
            {
                var accounts = await _unitOfWork.Repository<Account>().GetAll()
                                                .Where(x => x.CustomerId == customerId)
                                                .ToListAsync();
                Account account = null;
                switch (transactionType)
                {
                    case TransactionTypeEnum.Recharge:

                        if (accountType.Equals(AccountTypeEnum.PointAccount))
                        {
                            account = accounts.FirstOrDefault(x => x.Type == (int)AccountTypeEnum.PointAccount);
                            account.Balance += amount;
                            account.UpdateAt = DateTime.Now;
                            await _unitOfWork.Repository<Account>().UpdateDetached(account);
                        }
                        else if (accountType.Equals(AccountTypeEnum.CreditAccount))
                        {
                            account = accounts.FirstOrDefault(x => x.Type == (int)AccountTypeEnum.CreditAccount);
                            account.Balance += amount;
                            account.UpdateAt = DateTime.Now;
                        }

                        try
                        {
                            var transaction = new Transaction()
                            {
                                Id = Guid.NewGuid(),
                                AccountId = account.Id,
                                Amount = amount,
                                IsIncrease = true,
                                Status = (int)TransactionStatusEnum.Finish,
                                Type = (int)TransactionTypeEnum.Recharge,
                                CreatedAt = DateTime.Now
                            };

                            await _unitOfWork.Repository<Transaction>().InsertAsync(transaction);
                            await _unitOfWork.Repository<Account>().UpdateDetached(account);
                        }
                        catch (ErrorResponse ex)
                        {
                            throw new ErrorResponse(400, (int)TransactionErrorEnum.CREATE_TRANS_FAIL,
                                TransactionErrorEnum.CREATE_TRANS_FAIL.GetDisplayName());
                        }
                        break;

                    case TransactionTypeEnum.Payment:

                        account = accounts.FirstOrDefault(x => x.Type == (int)AccountTypeEnum.CreditAccount);

                        if (account.Balance < amount)
                            throw new ErrorResponse(400, (int)PaymentErrorsEnum.ERROR_BALANCE,
                                PaymentErrorsEnum.ERROR_BALANCE.GetDisplayName());

                        account.Balance -= amount;
                        account.UpdateAt = DateTime.Now;

                        try
                        {
                            var transaction = new Transaction()
                            {
                                Id = Guid.NewGuid(),
                                AccountId = account.Id,
                                Amount = amount,
                                IsIncrease = false,
                                Status = (int)TransactionStatusEnum.Finish,
                                Type = (int)TransactionTypeEnum.Payment,
                                CreatedAt = DateTime.Now
                            };

                            await _unitOfWork.Repository<Transaction>().InsertAsync(transaction);
                            await _unitOfWork.Repository<Account>().UpdateDetached(account);
                        }
                        catch (ErrorResponse ex)
                        {
                            throw new ErrorResponse(400, (int)TransactionErrorEnum.CREATE_TRANS_FAIL,
                                TransactionErrorEnum.CREATE_TRANS_FAIL.GetDisplayName());
                        }
                        break;
                }
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}

