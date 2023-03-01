using AutoMapper;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;

namespace FINE.Service.Service
{
    public interface IMembershipCardService
    {
        MembershipCard GetMembershipCardByCodeByBrandId(string cardCode, int campusId);
        IEnumerable<MembershipCard> GetMembershipCardActiveByCustomerIdAndBrandId(int customerId, int campusId);
        Task<MembershipCard> AddMembershipCard(string newCardCode, int campusId, int customerId);
        IQueryable<MembershipCard> GetMembershipCardActiveByCustomerId(int customerId);

    }
    public class MembershipCardService : IMembershipCardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private IMapper _mapper;
        public MembershipCardService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<MembershipCard> AddMembershipCard(string newCardCode, int campusId, int customerId)
        {
            var membership = new MembershipCard()
            {
                CustomerId = customerId,
                CardCode = newCardCode,
                CampusId = campusId,
                Type = (int)MembershipCardTypeEnum.Free,
                PhysicalCardCode= newCardCode,
                Active = true,
                CreateAt = DateTime.Now
            };
            await _unitOfWork.Repository<MembershipCard>().InsertAsync(membership);
            await _unitOfWork.CommitAsync();

            return membership;
        }

        public IQueryable<MembershipCard> GetMembershipCardActiveByCustomerId(int customerId)
        {
            return  _unitOfWork.Repository<MembershipCard>().GetAll()
                .Where(x => x.Active == true && x.CustomerId == customerId);             
        }

        public IEnumerable<MembershipCard> GetMembershipCardActiveByCustomerIdAndBrandId(int customerId, int campusId)
        {
            return _unitOfWork.Repository<MembershipCard>().GetAll()
                .Where(x => x.Active == true && x.CustomerId == customerId && x.CampusId == campusId).ToList();
        }

        public MembershipCard GetMembershipCardByCodeByBrandId(string cardCode, int campusId)
        {
            return _unitOfWork.Repository<MembershipCard>().GetAll().FirstOrDefault(x => x.CampusId == campusId && x.CardCode == cardCode);
        }
    }
}
