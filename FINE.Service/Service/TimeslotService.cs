using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Attributes;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Staff;
using FINE.Service.DTO.Request.TimeSlot;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface ITimeslotService
    {
        Task<BaseResponsePagingViewModel<TimeslotResponse>> GetTimeslotsByDestination(string destinationId, PagingRequest paging);
        Task<BaseResponseViewModel<TimeslotResponse>> CreateTimeslot(CreateTimeslotRequest request);
        Task<BaseResponseViewModel<TimeslotResponse>> UpdateTimeslot(string timeslotId, UpdateTimeslotRequest request);
    }

    public class TimeslotService : ITimeslotService
    {
        private IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public TimeslotService(IMapper mapper, IUnitOfWork unitOfWork)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponsePagingViewModel<TimeslotResponse>> GetTimeslotsByDestination(string destinationId, PagingRequest paging)
        {
            try
            {
                var destination = _unitOfWork.Repository<Destination>().GetAll()
                              .FirstOrDefault(x => x.Id == Guid.Parse(destinationId));
                if (destination == null)
                    throw new ErrorResponse(404, (int)DestinationErrorEnums.NOT_FOUND,
                        DestinationErrorEnums.NOT_FOUND.GetDisplayName());

                var timeslot = _unitOfWork.Repository<TimeSlot>().GetAll()
                                         .Where(x => x.DestinationId == destination.Id && x.IsActive == true)
                                         .ProjectTo<TimeslotResponse>(_mapper.ConfigurationProvider)
                                         .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

                return new BaseResponsePagingViewModel<TimeslotResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Page = paging.Page,
                        Size = paging.PageSize,
                        Total = timeslot.Item1
                    },
                    Data = timeslot.Item2.ToList()
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<TimeslotResponse>> CreateTimeslot(CreateTimeslotRequest request)
        {
            try
            {
                var checkDestination = _unitOfWork.Repository<Destination>().GetAll()
                                        .FirstOrDefault(x => x.Id == request.DestinationId);

                if (checkDestination == null)
                    throw new ErrorResponse(404, (int)DestinationErrorEnums.NOT_FOUND,
                                        DestinationErrorEnums.NOT_FOUND.GetDisplayName());

                var timeslot = _mapper.Map<CreateTimeslotRequest, TimeSlot>(request);

                timeslot.Id = Guid.NewGuid();
                timeslot.ArriveTime = request.ArriveTime.ToTimeSpan();
                timeslot.CheckoutTime = request.CheckoutTime.ToTimeSpan();
                timeslot.CloseTime = request.CloseTime.ToTimeSpan();
                timeslot.CreateAt = DateTime.Now;
                timeslot.IsActive = true;

                await _unitOfWork.Repository<TimeSlot>().InsertAsync(timeslot);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<TimeslotResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<TimeslotResponse>(timeslot)
                };
            }
            catch(ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<TimeslotResponse>> UpdateTimeslot(string timeslotId, UpdateTimeslotRequest request)
        {
            try
            {
                var checkTimeslot = _unitOfWork.Repository<TimeSlot>().Find(x => x.Id == Guid.Parse(timeslotId));
                if (checkTimeslot == null)
                {
                    throw new ErrorResponse(404, (int)TimeSlotErrorEnums.NOT_FOUND,
                                        TimeSlotErrorEnums.NOT_FOUND.GetDisplayName());
                }
                var timeslot = _mapper.Map<UpdateTimeslotRequest, TimeSlot>(request, checkTimeslot);
                timeslot.ArriveTime = request.ArriveTime.ToTimeSpan();
                timeslot.CheckoutTime = request.CheckoutTime.ToTimeSpan();
                timeslot.CloseTime = request.CloseTime.ToTimeSpan();
                timeslot.UpdateAt = DateTime.Now;

                await _unitOfWork.Repository<TimeSlot>().UpdateDetached(timeslot);
                await _unitOfWork.CommitAsync();
                return new BaseResponseViewModel<TimeslotResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<TimeslotResponse>(timeslot)
                };
            }
            catch(ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}
