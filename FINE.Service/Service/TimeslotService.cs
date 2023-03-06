using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Commons;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.TimeSlot;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using NTQ.Sdk.Core.Utilities;
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
        Task<BaseResponsePagingViewModel<TimeslotResponse>> GetTimeSlots(TimeslotResponse filter, PagingRequest paging);
        Task<BaseResponseViewModel<TimeslotResponse>> GetTimeSlotById(int timeslotId);
        Task<BaseResponseViewModel<TimeslotResponse>> CreateTimeslot(CreateTimeslotRequest request);
        Task<BaseResponseViewModel<TimeslotResponse>> UpdateTimeslot(int timeslotId, UpdateTimeslotRequest request);
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

        public async Task<BaseResponseViewModel<TimeslotResponse>> CreateTimeslot(CreateTimeslotRequest request)
        {
            var timeslot = _mapper.Map<CreateTimeslotRequest, TimeSlot>(request);
            timeslot.CreateAt = DateTime.Now;

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

        public async Task<BaseResponseViewModel<TimeslotResponse>> GetTimeSlotById(int timeslotId)
        {
            var timeslot = _unitOfWork.Repository<TimeSlot>().GetAll()
                                          .FirstOrDefault(x => x.Id == timeslotId);
            if (timeslot == null)
            {
                //throw new ErrorResponse(404, (int)TimeslotErrorEnums.NOT_FOUND_TIME,
                //                    TimeslotErrorEnums.NOT_FOUND_TIME.GetDisplayName());
            }
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

        public async Task<BaseResponsePagingViewModel<TimeslotResponse>> GetTimeSlots(TimeslotResponse filter, PagingRequest paging)
        {
            var timeslot = _unitOfWork.Repository<TimeSlot>().GetAll()
                                      .ProjectTo<TimeslotResponse>(_mapper.ConfigurationProvider)
                                      .DynamicFilter(filter)
                                      .DynamicSort(filter)
                                      .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging,
                                    Constants.DefaultPaging);
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

        public async Task<BaseResponseViewModel<TimeslotResponse>> UpdateTimeslot(int timeslotId, UpdateTimeslotRequest request)
        {
            TimeSlot timeslot = _unitOfWork.Repository<TimeSlot>()
                                            .Find(x => x.Id == timeslotId);
            //if(timeslot == null)
            //    throw new ErrorResponse(404, (int)TimeslotErrorEnums.NOT_FOUND_TIME,
            //                        TimeslotErrorEnums.NOT_FOUND_TIME.GetDisplayName());
            var timeslotMappingResult = _mapper.Map<UpdateTimeslotRequest, TimeSlot>(request, timeslot);
            await _unitOfWork.Repository<TimeSlot>().UpdateDetached(timeslotMappingResult);
            await _unitOfWork.CommitAsync();

            return new BaseResponseViewModel<TimeslotResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<TimeslotResponse>(timeslotMappingResult)
            };
        }
    }
}
