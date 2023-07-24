//using AutoMapper;
//using AutoMapper.QueryableExtensions;
//using FINE.Data.Entity;
//using FINE.Data.UnitOfWork;
//using FINE.Service.Attributes;
//using FINE.Service.DTO.Request;
//using FINE.Service.DTO.Request.TimeSlot;
//using FINE.Service.DTO.Response;
//using FINE.Service.Exceptions;
//using FINE.Service.Utilities;
//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net.NetworkInformation;
//using System.Text;
//using System.Threading.Tasks;
//using static FINE.Service.Helpers.ErrorEnum;

//namespace FINE.Service.Service
//{
//    public interface ITimeslotService
//    {
//        Task<BaseResponsePagingViewModel<TimeslotResponse>> GetTimeSlots(TimeslotResponse filter, PagingRequest paging);
//        Task<BaseResponseViewModel<TimeslotResponse>> GetTimeSlotById(int timeslotId);
//        Task<BaseResponsePagingViewModel<TimeslotResponse>> GetTimeslotsByDestination(int DestinationId, PagingRequest paging);
//        Task<BaseResponseViewModel<TimeslotResponse>> CreateTimeslot(CreateTimeslotRequest request);
//        Task<BaseResponseViewModel<TimeslotResponse>> UpdateTimeslot(int timeslotId, UpdateTimeslotRequest request);
//    }

//    public class TimeslotService : ITimeslotService
//    {
//        private IMapper _mapper;
//        private readonly IUnitOfWork _unitOfWork;

//        public TimeslotService(IMapper mapper, IUnitOfWork unitOfWork)
//        {
//            _mapper = mapper;
//            _unitOfWork = unitOfWork;
//        }

//        public async Task<BaseResponseViewModel<TimeslotResponse>> CreateTimeslot(CreateTimeslotRequest request)
//        {
//            try
//            {
//                var timeslot = _mapper.Map<CreateTimeslotRequest, TimeSlot>(request);

//                timeslot.ArriveTime = TimeSpan.Parse(request.ArriveTime.ToString("HH:mm:ss"));
//                timeslot.CheckoutTime = TimeSpan.Parse(request.CheckoutTime.ToString("HH:mm:ss"));
//                timeslot.CreateAt = DateTime.Now;

//                await _unitOfWork.Repository<TimeSlot>().InsertAsync(timeslot);
//                await _unitOfWork.CommitAsync();

//                return new BaseResponseViewModel<TimeslotResponse>()
//                {
//                    Status = new StatusViewModel()
//                    {
//                        Message = "Success",
//                        Success = true,
//                        ErrorCode = 0
//                    },
//                    Data = _mapper.Map<TimeslotResponse>(timeslot)
//                };
//            }
//            catch(Exception ex)
//            {
//                throw;
//            }
//        }

//        public async Task<BaseResponseViewModel<TimeslotResponse>> GetTimeSlotById(int timeslotId)
//        {
//            try
//            {
//                var timeslot = _unitOfWork.Repository<TimeSlot>().GetAll()
//                                              .FirstOrDefault(x => x.Id == timeslotId);
//                if (timeslot == null)
//                {
//                    throw new ErrorResponse(404, (int)TimeSlotErrorEnums.NOT_FOUND,
//                                        TimeSlotErrorEnums.NOT_FOUND.GetDisplayName());
//                }
//                return new BaseResponseViewModel<TimeslotResponse>()
//                {
//                    Status = new StatusViewModel()
//                    {
//                        Message = "Success",
//                        Success = true,
//                        ErrorCode = 0
//                    },
//                    Data = _mapper.Map<TimeslotResponse>(timeslot)
//                };
//            }
//            catch(Exception ex)
//            {
//                throw;
//            }
//        }

//        public async Task<BaseResponsePagingViewModel<TimeslotResponse>> GetTimeSlots(TimeslotResponse filter, PagingRequest paging)
//        {
//            try
//            {
//                var timeslot = _unitOfWork.Repository<TimeSlot>().GetAll()
//                                          .ProjectTo<TimeslotResponse>(_mapper.ConfigurationProvider)
//                                          .DynamicFilter(filter)
//                                          .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging,
//                                        Constants.DefaultPaging);
//                return new BaseResponsePagingViewModel<TimeslotResponse>()
//                {
//                    Metadata = new PagingsMetadata()
//                    {
//                        Page = paging.Page,
//                        Size = paging.PageSize,
//                        Total = timeslot.Item1
//                    },
//                    Data = timeslot.Item2.ToList()
//                };
//            }
//            catch(Exception ex)
//            {
//                throw;
//            }
//        }

//        public async Task<BaseResponseViewModel<TimeslotResponse>> UpdateTimeslot(int timeslotId, UpdateTimeslotRequest request)
//        {
//            try
//            {
//                var timeslot = _unitOfWork.Repository<TimeSlot>()
//                                                .Find(x => x.Id == timeslotId);
//                if (timeslot == null)
//                    throw new ErrorResponse(404, (int)TimeSlotErrorEnums.NOT_FOUND,
//                                        TimeSlotErrorEnums.NOT_FOUND.GetDisplayName());
                
//                timeslot.ArriveTime = TimeSpan.Parse(request.ArriveTime.ToString("HH:mm:ss"));
//                timeslot.CheckoutTime = TimeSpan.Parse(request.CheckoutTime.ToString("HH:mm:ss"));

//                var updateTimeslot = _mapper.Map<UpdateTimeslotRequest, TimeSlot>(request, timeslot);
//                updateTimeslot.UpdateAt = DateTime.Now;  

//                await _unitOfWork.Repository<TimeSlot>().UpdateDetached(updateTimeslot);
//                await _unitOfWork.CommitAsync();

//                return new BaseResponseViewModel<TimeslotResponse>()
//                {
//                    Status = new StatusViewModel()
//                    {
//                        Message = "Success",
//                        Success = true,
//                        ErrorCode = 0
//                    },
//                    Data = _mapper.Map<TimeslotResponse>(updateTimeslot)
//                };
//            }
//            catch(Exception ex)
//            {
//                throw;
//            }
//        }

//        public async Task<BaseResponsePagingViewModel<TimeslotResponse>> GetTimeslotsByDestination(int DestinationId, PagingRequest paging)
//        {
//            try
//            {
//                #region check Destination exist
//                var checkDestination = _unitOfWork.Repository<Destination>().GetAll()
//                              .FirstOrDefault(x => x.Id == DestinationId);
//                if (checkDestination == null)
//                    throw new ErrorResponse(404, (int)DestinationErrorEnums.NOT_FOUND_ID,
//                        DestinationErrorEnums.NOT_FOUND_ID.GetDisplayName());
//                #endregion

//                var timeslot = _unitOfWork.Repository<TimeSlot>().GetAll()
//                 .Where(x => x.DestinationId == DestinationId)

//                 .ProjectTo<TimeslotResponse>(_mapper.ConfigurationProvider)
//                 .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

//                return new BaseResponsePagingViewModel<TimeslotResponse>()
//                {
//                    Metadata = new PagingsMetadata()
//                    {
//                        Page = paging.Page,
//                        Size = paging.PageSize,
//                        Total = timeslot.Item1
//                    },
//                    Data = timeslot.Item2.ToList()
//                };
//            }
//            catch(Exception ex)
//            {
//                throw;
//            }
//        }
//    }
//}
