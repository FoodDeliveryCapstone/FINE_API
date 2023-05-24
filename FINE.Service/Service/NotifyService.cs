using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Commons;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Noti;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using NetTopologySuite.Algorithm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface INotifyService
    {
        Task<BaseResponsePagingViewModel<NotifyResponse>> GetNotifys(NotifyResponse filter, PagingRequest paging);
        Task<BaseResponseViewModel<NotifyResponse>> GetNotifyById(int notifyId);
        Task<bool> CreateOrderNotify(NotifyRequestModel request);
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

        public async Task<bool> CreateOrderNotify(NotifyRequestModel request)
        {
            if (request.CustomerId == null)
            {
                throw new ErrorResponse(404, (int)NotifyErrorEnum.NOT_FOUND_ID,
                                    NotifyErrorEnum.NOT_FOUND_ID.GetDisplayName());
            }

            if (request.Type == (int)NotifyTypeEnum.ForOrder)
            {
                switch (request.OrderStatus)
                {
                    case (int)OrderStatusEnum.Processing:
                        request.Title = "Trạng thái đơn hàng";
                        request.Description = $"Đơn hàng mới {request.OrderCode} ";
                        break;
                    case (int)OrderStatusEnum.Finished:
                        request.Title = "Trạng thái đơn hàng";
                        request.Description = $"Đơn hàng {request.OrderCode} hoàn thành";
                        break;
                    case (int)OrderStatusEnum.UserCancel:
                        request.Title = "Trạng thái đơn hàng";
                        request.Description = $"Đơn hàng {request.OrderCode} đã hủy";
                        break;
                    //case (int)OrderStatusEnum.OnlinePaid:
                    //    request.Title = "Trạng thái đơn hàng";
                    //    request.Description = $"Đơn hàng {request.OrderId} đã thanh toán ";
                    //    break;
                }
            }
            else if (request.Type == (int)NotifyTypeEnum.ForGift)
            {
                if (String.IsNullOrEmpty(request.Title))
                {
                    request.Title = "";
                }
                if (String.IsNullOrEmpty(request.Description))
                {
                    request.Description = "";
                }
            }
            try
            {
                _unitOfWork.Repository<Notify>().Insert(new Notify
                {
                    CustomerId = request.CustomerId,
                    Title = request.Title,
                    Description = request.Description,
                    IsRead = false,
                    Active = true,
                    CreateAt = DateTime.Now
                });

            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
            return true;
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
