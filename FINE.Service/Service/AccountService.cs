using AutoMapper;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.Service
{
    public interface IAccountService
    {
        void CreateAccountByMemCard(string cardCode, double? defaultBalance, int uniId, int cardId, int accTypeId);
        Task<List<Account>> GetAccountByCustomerId(int id);
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

        public void CreateAccountByMemCard(string cardCode, double? defaultBalance, int uniId, int cardId, int accTypeId)
        {
            try
            {
                var university = _unitOfWork.Repository<University>().GetById(uniId).Result;
                Account account = new Account()
                {
                    MembershipCardId = cardId,
                    AccountCode = university.UniCode + "-" + DateTime.Now.ToString("yyyyMMddHHmmssfff"),
                    AccountName = university.UniCode + "-" + accTypeId + "-" + cardCode,
                    StartDate = DateTime.Now,
                    Balance = (decimal)(defaultBalance ?? 0),
                    Type = accTypeId,
                    CampusId = uniId,
                    Active = true
                };

                _unitOfWork.Repository<Account>().Insert(account);
                _unitOfWork.Commit();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<List<Account>> GetAccountByCustomerId(int id)
        {
            return await _unitOfWork.Repository<Account>().GetAll().Where(x => x.MembershipCard.CustomerId == id).ToListAsync();
        }
    }
}
