using AutoMapper;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Commons;
using FirebaseAdmin.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.Service
{
    public interface IFcmTokenService
    {
        Task<Fcmtoken> AddFcmToken(string fcmToken, int customerId);
        Fcmtoken AddStaffFcmToken(string fcmToken, int staffId);

        int RemoveFcmTokens(ICollection<string> fcmTokens);

        void UnsubscribeAll(int customerId);

        void SubscribeAll(int customerId);

        Task<bool> ValidToken(string fcmToken);
    }
    public class FcmTokenService : IFcmTokenService
    {
        private readonly IFirebaseMessagingService _fmService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public FcmTokenService(IUnitOfWork unitOfWork, IMapper mapper, IFirebaseMessagingService fmService)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _fmService = fmService;
        }

        public async Task<Fcmtoken> AddFcmToken(string fcmToken, int customerId)
        {
            var fcm = _unitOfWork.Repository<Fcmtoken>().GetAll()
                .FirstOrDefault(x => x.Token.Equals(fcmToken));

            if (fcm == null)
            {
                _fmService.Subcribe(new List<string>() { fcmToken }, Constants.NOTIFICATION_TOPIC);

                var newtoken = new Fcmtoken()
                {
                    Token = fcmToken,
                    CustomerId = customerId
                };
                await _unitOfWork.Repository<Fcmtoken>().InsertAsync(newtoken);
                await _unitOfWork.CommitAsync();
            }
            else if (!fcm.CustomerId.Equals(customerId))
            {
                _fmService.Subcribe(new List<string>() { fcmToken }, Constants.NOTIFICATION_TOPIC);

                fcm.CustomerId = customerId;
                await _unitOfWork.Repository<Fcmtoken>().Update(fcm, fcm.Id);
                await _unitOfWork.CommitAsync();
            }
            return fcm;
        }

        public Fcmtoken AddStaffFcmToken(string fcmToken, int staffId)
        {
            var fcm = _unitOfWork.Repository<Fcmtoken>().GetAll().FirstOrDefault(x => x.Token.Equals(fcmToken));

            if (fcm == null)
            {
                _unitOfWork.Repository<Fcmtoken>().Insert(new Fcmtoken() { Token = fcmToken, StaffId = staffId, CreateAt = DateTime.Now });
                _unitOfWork.Commit();
            }
            else if (!fcm.CustomerId.Equals(staffId))
            {
                fcm.StaffId = staffId;
                _unitOfWork.Repository<Fcmtoken>().UpdateDetached(fcm);
                _unitOfWork.Commit();
            }

            return fcm;
        }

        public int RemoveFcmTokens(ICollection<string> fcmTokens)
        {
            var tokens = _unitOfWork.Repository<Fcmtoken>().GetAll();

            if (tokens == null)
                return 0;

            var cusTokens = tokens.Where(x => x.CustomerId != null).Select(x => x.Token).ToList();
            if (cusTokens != null && cusTokens.Count > 0)
                _fmService.Unsubcribe(cusTokens, Constants.NOTIFICATION_TOPIC);

            _unitOfWork.Repository<Fcmtoken>().DeleteRange(tokens);
            _unitOfWork.Commit();

            return tokens.Count();
        }

        public void SubscribeAll(int customerId)
        {
            var tokensMapping = _unitOfWork.Repository<Fcmtoken>().GetAll().Where(x => x.CustomerId.Equals(customerId));
            if (tokensMapping != null)
            {
                var tokens = tokensMapping.Select(x => x.Token).ToList();
                _fmService.Subcribe(tokens, Constants.NOTIFICATION_TOPIC);
            }
        }

        public void UnsubscribeAll(int customerId)
        {
            var tokensMapping = _unitOfWork.Repository<Fcmtoken>().GetAll().Where(x => x.CustomerId.Equals(customerId));
            if (tokensMapping != null)
            {
                var tokens = tokensMapping.Select(x => x.Token).ToList();
                _fmService.Unsubcribe(tokens, Constants.NOTIFICATION_TOPIC);
            }
        }

        public async Task<bool> ValidToken(string fcmToken)
        {
            return await _fmService.ValidToken(fcmToken);
        }
    }
}
