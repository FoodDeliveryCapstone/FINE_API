using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Commons;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Product;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using Microsoft.EntityFrameworkCore;
using NTQ.Sdk.Core.Utilities;
using System.Linq.Dynamic.Core;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface ITimeSlotService
    {
        Task<BaseResponsePagingViewModel<TimeSlotResponse>> GetProductByStoreAndTimeslot(int storeId, int timeslotId, PagingRequest paging);
        Task<BaseResponsePagingViewModel<TimeSlotResponse>> GetProductByTimeslot(int timeslotId, PagingRequest paging);
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

        public async Task<BaseResponsePagingViewModel<TimeSlotResponse>> GetProductByStoreAndTimeslot(int storeId, int timeslotId, PagingRequest paging)
        {
            #region check store and timeslot exist
            var checkStore = _unitOfWork.Repository<Store>().GetAll()
                          .FirstOrDefault(x => x.Id == storeId);
            if (checkStore == null)
                throw new ErrorResponse(404, (int)StoreErrorEnums.NOT_FOUND_ID,
                    StoreErrorEnums.NOT_FOUND_ID.GetDisplayName());

            var checkTimeslot = _unitOfWork.Repository<TimeSlot>().GetAll()
                          .FirstOrDefault(x => x.Id == timeslotId);
            if (checkTimeslot == null)
                throw new ErrorResponse(404, (int)TimeSlotErrorEnums.NOT_FOUND_ID,
                    TimeSlotErrorEnums.NOT_FOUND_ID.GetDisplayName());
            #endregion

            var timeslot = _unitOfWork.Repository<TimeSlot>().GetAll()

               .Include(x => x.Campus)
               .ThenInclude(x => x.Stores)
               .ThenInclude(x => x.Products)
               .Where(x => x.Id == timeslotId && (x.Campus.Stores.Any(y => y.Id == storeId)))

               .ProjectTo<TimeSlotResponse>(_mapper.ConfigurationProvider)
               .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);
       
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

        public async Task<BaseResponsePagingViewModel<TimeSlotResponse>> GetProductByTimeslot(int timeslotId, PagingRequest paging)
        {
            #region check timeslot exist
            var checkTimeslot = _unitOfWork.Repository<TimeSlot>().GetAll()
                          .FirstOrDefault(x => x.Id == timeslotId);
            if (checkTimeslot == null)
                throw new ErrorResponse(404, (int)TimeSlotErrorEnums.NOT_FOUND_ID,
                    TimeSlotErrorEnums.NOT_FOUND_ID.GetDisplayName());
            #endregion

            var timeslot = _unitOfWork.Repository<TimeSlot>().GetAll()

             .Include(x => x.Menus)
             .ThenInclude(x => x.ProductInMenus)
             .ThenInclude(x => x.Product)
             .Where(x => x.Id == timeslotId)

             .ProjectTo<TimeSlotResponse>(_mapper.ConfigurationProvider)
             .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

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
