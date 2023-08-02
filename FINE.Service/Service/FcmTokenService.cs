using AutoMapper;
using Castle.Core.Resource;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Attributes;
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
        void AddFcmToken(string fcmToken, Guid customerId);

        //void AddStaffFcmToken(string fcmToken, string staffId);

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

        public void AddFcmToken(string fcmToken, Guid customerId)
        {
            try
            {
                var fcm = _unitOfWork.Repository<Fcmtoken>().GetAll()
                    .FirstOrDefault(x =>x.CustomerId == customerId);

                if (fcm == null)
                {
                    //_fmService.Subcribe(new List<string>() { fcmToken }, Constants.NOTIFICATION_TOPIC);
                    var newtoken = new Fcmtoken()
                    {
                        Id = Guid.NewGuid(),
                        Token = fcmToken,
                        CustomerId = customerId,
                        CreateAt = DateTime.UtcNow
                    };
                    _unitOfWork.Repository<Fcmtoken>().Insert(newtoken);
                    _unitOfWork.Commit();
                }
                else if (!fcm.Token.Equals(fcmToken))
                {
                    //_fmService.Subcribe(new List<string>() { fcmToken }, Constants.NOTIFICATION_TOPIC);
                    fcm.Token = fcmToken;
                    fcm.UpdateAt= DateTime.Now;

                    _unitOfWork.Repository<Fcmtoken>().UpdateDetached(fcm);
                    _unitOfWork.Commit();
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        //public void AddStaffFcmToken(string fcmToken, Guid staffId)
        //{
        //    try
        //    {
        //        var fcm = _unitOfWork.Repository<Fcmtoken>().GetAll()
        //            .FirstOrDefault(x => x.StaffId == staffId);

        //        if (fcm == null)
        //        {
        //            _fmService.Subcribe(new List<string>() { fcmToken }, Constants.NOTIFICATION_TOPIC);

        //            var newtoken = new Fcmtoken()
        //            {
        //                Token = fcmToken,
        //                StaffId = staffId,
        //                CreateAt = DateTime.UtcNow
        //            };
        //            _unitOfWork.Repository<Fcmtoken>().Insert(newtoken);
        //            _unitOfWork.Commit();
        //        }
        //        else if (!fcm.Token.Equals(fcmToken))
        //        {
        //            _fmService.Subcribe(new List<string>() { fcmToken }, Constants.NOTIFICATION_TOPIC);

        //            fcm.StaffId = staffId;
        //            fcm.UpdateAt = DateTime.UtcNow;

        //            _unitOfWork.Repository<Fcmtoken>().Update(fcm, fcm.Id);
        //            _unitOfWork.Commit();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //    }
        //}

        public int RemoveFcmTokens(ICollection<string> fcmTokens)
        {
            try
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
            catch (Exception ex)
            {
                throw;
            }
        }

        public void SubscribeAll(int customerId)
        {
            try
            {
                var tokensMapping = _unitOfWork.Repository<Fcmtoken>().GetAll().Where(x => x.CustomerId.Equals(customerId));
                if (tokensMapping != null)
                {
                    var tokens = tokensMapping.Select(x => x.Token).ToList();
                    _fmService.Subcribe(tokens, Constants.NOTIFICATION_TOPIC);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public void UnsubscribeAll(int customerId)
        {
            try
            {
                var tokensMapping = _unitOfWork.Repository<Fcmtoken>().GetAll().Where(x => x.CustomerId.Equals(customerId));
                if (tokensMapping != null)
                {
                    var tokens = tokensMapping.Select(x => x.Token).ToList();
                    _fmService.Unsubcribe(tokens, Constants.NOTIFICATION_TOPIC);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> ValidToken(string fcmToken)
        {
            return await _fmService.ValidToken(fcmToken);
        }
    }
}
