using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Commons;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.TimeSlot;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using Microsoft.EntityFrameworkCore;
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
    public interface ITimeSlotService
    {
        Task<BaseResponsePagingViewModel<TimeSlotResponse>> GetTimeSlots(TimeSlotResponse filter, PagingRequest paging);
        Task<BaseResponsePagingViewModel<TimeSlotResponse>> GetProductByTimeSlot(int timeslotId, PagingRequest paging);
        Task<BaseResponseViewModel<TimeSlotResponse>> GetTimeSlotById(int timeslotId);
        Task<BaseResponseViewModel<TimeSlotResponse>> CreateTimeslot(CreateTimeslotRequest request);
        Task<BaseResponseViewModel<TimeSlotResponse>> UpdateTimeslot(int timeslotId, UpdateTimeslotRequest request);
    }

    public class TimeSlotService : ITimeSlotService
    {
        private IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public TimeSlotService(IMapper mapper, IUnitOfWork unitOfWork)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponseViewModel<TimeSlotResponse>> CreateTimeslot(CreateTimeslotRequest request)
        {
            var timeslot = _mapper.Map<CreateTimeslotRequest, TimeSlot>(request);
            timeslot.CreateAt = DateTime.Now;

            await _unitOfWork.Repository<TimeSlot>().InsertAsync(timeslot);
            await _unitOfWork.CommitAsync();

            return new BaseResponseViewModel<TimeSlotResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<TimeSlotResponse>(timeslot)
            };
        }

        public async Task<BaseResponseViewModel<TimeSlotResponse>> GetTimeSlotById(int timeslotId)
        {
            var timeslot = _unitOfWork.Repository<TimeSlot>().GetAll()
                                          .FirstOrDefault(x => x.Id == timeslotId);
            if (timeslot == null)
            {
                //throw new ErrorResponse(404, (int)TimeslotErrorEnums.NOT_FOUND_TIME,
                //                    TimeslotErrorEnums.NOT_FOUND_TIME.GetDisplayName());
            }
            return new BaseResponseViewModel<TimeSlotResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<TimeSlotResponse>(timeslot)
            };
        }

        public async Task<BaseResponsePagingViewModel<TimeSlotResponse>> GetTimeSlots(TimeSlotResponse filter, PagingRequest paging)
        {
            var timeslot = _unitOfWork.Repository<TimeSlot>().GetAll()
                                      .ProjectTo<TimeSlotResponse>(_mapper.ConfigurationProvider)
                                      .DynamicFilter(filter)
                                      .DynamicSort(filter)
                                      .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging,
                                    Constants.DefaultPaging);
            return new BaseResponsePagingViewModel<TimeSlotResponse>()
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

        public async Task<BaseResponseViewModel<TimeSlotResponse>> UpdateTimeslot(int timeslotId, UpdateTimeslotRequest request)
        {
            TimeSlot timeslot = _unitOfWork.Repository<TimeSlot>()
                                            .Find(x => x.Id == timeslotId);
            //if(timeslot == null)
            //    throw new ErrorResponse(404, (int)TimeslotErrorEnums.NOT_FOUND_TIME,
            //                        TimeslotErrorEnums.NOT_FOUND_TIME.GetDisplayName());
            var timeslotMappingResult = _mapper.Map<UpdateTimeslotRequest, TimeSlot>(request, timeslot);
            await _unitOfWork.Repository<TimeSlot>().UpdateDetached(timeslotMappingResult);
            await _unitOfWork.CommitAsync();

            return new BaseResponseViewModel<TimeSlotResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<TimeSlotResponse>(timeslotMappingResult)
            };
        }

        public async Task<BaseResponsePagingViewModel<TimeSlotResponse>> GetProductByTimeSlot(int timeslotId, PagingRequest paging)
        {
            var timeslot = _unitOfWork.Repository<TimeSlot>().GetAll()
                                          .Include(x => x.ProductCollectionTimeSlots)
                                          .ThenInclude(x => x.ProductCollection)
                                          .ThenInclude(x => x.ProductionCollectionItems)
                                          .ThenInclude(x => x.Product)
                                          .Where(x => x.Id == timeslotId)
                                          .ProjectTo<TimeSlotResponse>(_mapper.ConfigurationProvider)
                                          .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

            #region Check Timeslot
            //if (timeslot == null)
            //{
            //    throw new ErrorResponse(404, (int)TimeslotErrorEnums.NOT_FOUND_TIME,
            //                        TimeslotErrorEnums.NOT_FOUND_TIME.GetDisplayName());
            //}
            #endregion

            return new BaseResponsePagingViewModel<TimeSlotResponse>()
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
    }
}
