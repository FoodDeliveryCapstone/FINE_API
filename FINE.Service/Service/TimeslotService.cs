using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Attributes;
using FINE.Service.DTO.Request;
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
        Task<BaseResponseViewModel<List<ProductResponse>>> GetProductsInTimeSlot(string timeSlotId);
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
    }
}
