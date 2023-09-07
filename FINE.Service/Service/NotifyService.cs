using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Attributes;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Noti;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using Microsoft.EntityFrameworkCore;
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
        Task<Notify> CreateOrderNotify(NotifyRequestModel request);
        Task<BaseResponseViewModel<dynamic>> UpdateIsReadForNotify(string? notifyId, bool? isRead);
        Task<BaseResponseViewModel<List<NotifyResponse>>> GetAllNotifyForUser(string customerId);
        Task<BaseResponseViewModel<NotifyResponse>> GetNotifyForUserById(string notifyId);
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

        public async Task<Notify> CreateOrderNotify(NotifyRequestModel request)
        {
            try
            {
                var newNotify = new Notify
                {
                    Id = Guid.NewGuid(),
                    CustomerId = request.CustomerId,
                    IsRead = false,
                    IsActive = true,
                    CreateAt = DateTime.Now,
                };

                if (request.Type == NotifyTypeEnum.ForOrder)
                {
                    switch (request.OrderStatus)
                    {
                        case OrderStatusEnum.Processing:
                            newNotify.Title = "Trạng thái đơn hàng";
                            newNotify.Description = $"Đơn hàng mới {request.OrderCode} ";
                            break;
                        case OrderStatusEnum.Finished:
                            newNotify.Title = "Trạng thái đơn hàng";
                            newNotify.Description = $"Đơn hàng {request.OrderCode} hoàn thành";
                            break;
                        case OrderStatusEnum.UserCancel:
                            newNotify.Title = "Trạng thái đơn hàng";
                            newNotify.Description = $"Đơn hàng {request.OrderCode} đã hủy";
                            break;
                    }
                }
                await _unitOfWork.Repository<Notify>().InsertAsync(newNotify);
                await _unitOfWork.CommitAsync();

                return newNotify;
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<dynamic>> UpdateIsReadForNotify(string? notifyId, bool? isRead)
        {
            try
            {
                var notify = await _unitOfWork.Repository<Notify>().GetAll().FirstOrDefaultAsync(x => x.Id == Guid.Parse(notifyId));

                if (notify == null)
                    throw new ErrorResponse(404, (int)NotifyErrorEnum.NOT_FOUND,
                        NotifyErrorEnum.NOT_FOUND.GetDisplayName());

                notify.IsRead = (bool)isRead;

                await _unitOfWork.Repository<Notify>().UpdateDetached(notify);
                await _unitOfWork.CommitAsync();

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

        public async Task<BaseResponseViewModel<List<NotifyResponse>>> GetAllNotifyForUser(string customerId)
        {
            try
            {
                var notify = await _unitOfWork.Repository<Notify>().GetAll()
                                    .Where(x => x.Customer.Id == Guid.Parse(customerId))
                                    .ProjectTo<NotifyResponse>(_mapper.ConfigurationProvider)
                                    .ToListAsync();

                return new BaseResponseViewModel<List<NotifyResponse>>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = notify
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<NotifyResponse>> GetNotifyForUserById(string notifyId)
        {
            var notify = await _unitOfWork.Repository<Notify>().GetAll()
                            .Where(x => x.Id == Guid.Parse(notifyId))
                            .ProjectTo<NotifyResponse>(_mapper.ConfigurationProvider)
                            .FirstOrDefaultAsync();

            if (notify == null)
                throw new ErrorResponse(404, (int)NotifyErrorEnum.NOT_FOUND,
                    NotifyErrorEnum.NOT_FOUND.GetDisplayName());

            return new BaseResponseViewModel<NotifyResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = notify
            };
        }
    }
}
