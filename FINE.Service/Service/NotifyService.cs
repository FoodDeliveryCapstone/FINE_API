using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Commons;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Noti;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using NetTopologySuite.Algorithm;
using NTQ.Sdk.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface INotifyService
    {
        Task<BaseResponsePagingViewModel<NotifyResponse>> GetNotifys(NotifyResponse filter, PagingRequest paging);
        Task<BaseResponseViewModel<NotifyResponse>> GetNotifyById(int notifyId);
        Task<BaseResponseViewModel<NotifyResponse>> CreateNotify(CreateNotifyRequest request);
        Task<BaseResponseViewModel<NotifyResponse>> UpdateNotify(int notifyId, UpdateNotifyRequest reuqest);
    }

    public class NotifyService : INotifyService
    {
        private IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public NotifyService(IMapper mapepr, IUnitOfWork unitOfWork)
        {
            _mapper = mapepr;
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponseViewModel<NotifyResponse>> CreateNotify(CreateNotifyRequest request)
        {
            var notify = _mapper.Map<CreateNotifyRequest, Notify>(request);
            notify.IsRead = false;
            notify.Active = true;
            notify.CreateAt = DateTime.Now;

            await _unitOfWork.Repository<Notify>().InsertAsync(notify);
            await _unitOfWork.CommitAsync();

            return new BaseResponseViewModel<NotifyResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<NotifyResponse>(notify)
            };
        }

        public async Task<BaseResponseViewModel<NotifyResponse>> GetNotifyById(int notifyId)
        {
            var notify = _unitOfWork.Repository<Notify>().GetAll()
                                    .FirstOrDefault(x => x.Id == notifyId);
            if (notify == null)
            {
                throw new ErrorResponse(404, (int)NotifyErrorEnum.NOT_FOUND_ID,
                                    NotifyErrorEnum.NOT_FOUND_ID.GetDisplayName());
            }
            return new BaseResponseViewModel<NotifyResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<NotifyResponse>(notify)
            };
        }

        public async Task<BaseResponsePagingViewModel<NotifyResponse>> GetNotifys(NotifyResponse filter, PagingRequest paging)
        {
            var notify = _unitOfWork.Repository<Notify>().GetAll()
                                     .ProjectTo<NotifyResponse>(_mapper.ConfigurationProvider)
                                       .DynamicFilter(filter)
                                       .DynamicSort(filter)
                                       .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging,
                                    Constants.DefaultPaging);
            return new BaseResponsePagingViewModel<NotifyResponse>()
            {
                Metadata = new PagingsMetadata()
                {
                    Page = paging.Page,
                    Size = paging.PageSize,
                    Total = notify.Item1
                },
                Data = notify.Item2.ToList()
            };
        }

        public async Task<BaseResponseViewModel<NotifyResponse>> UpdateNotify(int notifyId, UpdateNotifyRequest reuqest)
        {
            var notify = _unitOfWork.Repository<Notify>().Find(x => x.Id == notifyId);
            if(notify == null)
            {
                throw new ErrorResponse(400, (int)NotifyErrorEnum.NOT_FOUND_ID,
                                    NotifyErrorEnum.NOT_FOUND_ID.GetDisplayName());
            }

            var notifyMappingResult = _mapper.Map<UpdateNotifyRequest, Notify>(reuqest, notify);
            notifyMappingResult.UpdateAt = DateTime.Now;

            await _unitOfWork.Repository<Notify>()
                             .UpdateDetached(notifyMappingResult);
            await _unitOfWork.CommitAsync();

            return new BaseResponseViewModel<NotifyResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<NotifyResponse>(notifyMappingResult)
            };

        }
    }
}
