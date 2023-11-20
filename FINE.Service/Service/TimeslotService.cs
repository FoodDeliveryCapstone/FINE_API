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
using FINE.Service.Helpers;
using FINE.Service.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        Task<BaseResponsePagingViewModel<ListTimeslotResponse>> GetTimeslotsByDestination(string destinationId, PagingRequest paging);
        Task<BaseResponseViewModel<TimeSlotResponse>> UserGetListTimeslot(string destinationId);
        Task<BaseResponseViewModel<List<ProductResponse>>> GetProductsInTimeSlot(string timeSlotId);
        Task<BaseResponseViewModel<ListTimeslotResponse>> CreateTimeslot(CreateTimeslotRequest request);
        Task<BaseResponseViewModel<ListTimeslotResponse>> UpdateTimeslot(string timeslotId, UpdateTimeslotRequest request);
    }

    public class TimeslotService : ITimeslotService
    {
        private IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;

        public TimeslotService(IMapper mapper, IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
        }

        public async Task<BaseResponsePagingViewModel<ListTimeslotResponse>> GetTimeslotsByDestination(string destinationId, PagingRequest paging)
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
                                         .OrderBy(x => x.ArriveTime)
                                         .ProjectTo<ListTimeslotResponse>(_mapper.ConfigurationProvider)
                                         .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

                return new BaseResponsePagingViewModel<ListTimeslotResponse>()
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

        public async Task<BaseResponseViewModel<TimeSlotResponse>> UserGetListTimeslot(string destinationId)
        {
            try
            {
                var boxSize = new CubeModel()
                {
                    Height = double.Parse(_configuration.GetSection("BoxSize:Height").Value.ToString()),
                    Width = double.Parse(_configuration.GetSection("BoxSize:Width").Value.ToString()),
                    Length = double.Parse(_configuration.GetSection("BoxSize:Depth").Value.ToString())
                };
                var result = new TimeSlotResponse()
                {
                    BoxSize = boxSize,
                    MaxQuantityInBox = Int32.Parse(_configuration["MaxQuantityInBox"])
                };

                var destination = await _unitOfWork.Repository<Destination>().GetAll()
                              .FirstOrDefaultAsync(x => x.Id == Guid.Parse(destinationId));

                if (destination == null)
                    throw new ErrorResponse(404, (int)DestinationErrorEnums.NOT_FOUND,
                        DestinationErrorEnums.NOT_FOUND.GetDisplayName());

                result.ListTimeslotResponse = _unitOfWork.Repository<TimeSlot>().GetAll()
                                                         .Where(x => x.DestinationId == destination.Id && x.IsActive == true)
                                                         .OrderBy(x => x.ArriveTime)
                                                         .ProjectTo<ListTimeslotResponse>(_mapper.ConfigurationProvider)
                                                         .ToList();

                return new BaseResponseViewModel<TimeSlotResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = result
                };

            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
        public async Task<BaseResponseViewModel<List<ProductResponse>>> GetProductsInTimeSlot(string timeSlotId)
        {
            try
            {
                var products = await _unitOfWork.Repository<ProductInMenu>().GetAll()
                                               .Include(x => x.Product)
                                               .ThenInclude(x => x.Product)
                                               .Where(x => x.Menu.TimeSlotId == Guid.Parse(timeSlotId))
                                                .GroupBy(x => x.Product.Product)
                                                .Select(x => _mapper.Map<ProductResponse>(x.Key))
                                                .ToListAsync();

                return new BaseResponseViewModel<List<ProductResponse>>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = products
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<ListTimeslotResponse>> CreateTimeslot(CreateTimeslotRequest request)
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

                return new BaseResponseViewModel<ListTimeslotResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<ListTimeslotResponse>(timeslot)
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<ListTimeslotResponse>> UpdateTimeslot(string timeslotId, UpdateTimeslotRequest request)
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
                return new BaseResponseViewModel<ListTimeslotResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<ListTimeslotResponse>(timeslot)
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}
