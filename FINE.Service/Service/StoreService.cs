using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Commons;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Store;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using Microsoft.EntityFrameworkCore;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IStoreService
    {
        Task<BaseResponsePagingViewModel<StoreResponse>> GetStores(StoreResponse filter, PagingRequest paging);
        Task<BaseResponseViewModel<StoreResponse>> GetStoreById(int storeId);
        Task<BaseResponsePagingViewModel<StoreResponse>> GetStoreByTimeslot(int timeslotId, PagingRequest paging);
        Task<BaseResponseViewModel<StoreResponse>> CreateStore(CreateStoreRequest request);
        Task<BaseResponseViewModel<StoreResponse>> UpdateStore(int storeId, UpdateStoreRequest request);
    }

    public class StoreService : IStoreService
    {
        private IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public StoreService(IMapper mapper, IUnitOfWork unitOfWork)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponseViewModel<StoreResponse>> CreateStore(CreateStoreRequest request)
        {
            try
            {
                var store = _mapper.Map<CreateStoreRequest, Store>(request);

                store.IsActive = true;
                store.CreatedAt = DateTime.Now;

                await _unitOfWork.Repository<Store>().InsertAsync(store);
                await _unitOfWork.CommitAsync();
                return new BaseResponseViewModel<StoreResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<StoreResponse>(store)
                };
            }
            catch(ErrorResponse ex)
            {
                throw;
            }
        }

        public async Task<BaseResponseViewModel<StoreResponse>> GetStoreById(int storeId)
        {
            try
            {
                var store = _unitOfWork.Repository<Store>().GetAll()
                                            .FirstOrDefault(x => x.Id == storeId);
                if (store == null)
                    throw new ErrorResponse(404, (int)StoreErrorEnums.NOT_FOUND_ID,
                                        StoreErrorEnums.NOT_FOUND_ID.GetDisplayName());
                return new BaseResponseViewModel<StoreResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<StoreResponse>(store)
                };
            }
            catch(ErrorResponse ex)
            {
                throw;
            }
        }

        public async Task<BaseResponsePagingViewModel<StoreResponse>> GetStores(StoreResponse filter, PagingRequest paging)
        {
            try
            {
                var store = _unitOfWork.Repository<Store>().GetAll()
                                        .ProjectTo<StoreResponse>(_mapper.ConfigurationProvider)
                                        .DynamicFilter(filter)
                                        .DynamicSort(filter)
                                        .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging,
                                        Constants.DefaultPaging);
                return new BaseResponsePagingViewModel<StoreResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Page = paging.Page,
                        Size = paging.PageSize,
                        Total = store.Item1
                    },
                    Data = store.Item2.ToList()
                };
            }
            catch (ErrorResponse ex)
            {
                throw;
            }
        }

        public async Task<BaseResponseViewModel<StoreResponse>> UpdateStore(int storeId, UpdateStoreRequest request)
        {
            try
            {
                var store = _unitOfWork.Repository<Store>().GetAll()
                .FirstOrDefault(x => x.Id == storeId);

                if (store == null)
                    throw new ErrorResponse(404, (int)StoreErrorEnums.NOT_FOUND_ID,
                        StoreErrorEnums.NOT_FOUND_ID.GetDisplayName());

                var updateStore = _mapper.Map<UpdateStoreRequest, Store>(request, store);

                updateStore.UpdatedAt = DateTime.Now;

                await _unitOfWork.Repository<Store>().UpdateDetached(updateStore);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<StoreResponse>()
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
                throw;
            }
        }

        public async Task<BaseResponsePagingViewModel<StoreResponse>> GetStoreByTimeslot(int timeslotId, PagingRequest paging)
        {
            try
            {
                #region timeslot exsist
                var checkTimeslot = _unitOfWork.Repository<TimeSlot>().GetAll()
                              .FirstOrDefault(x => x.Id == timeslotId);
                if (checkTimeslot == null)
                    throw new ErrorResponse(404, (int)TimeSlotErrorEnums.NOT_FOUND_ID,
                        TimeSlotErrorEnums.NOT_FOUND_ID.GetDisplayName());
                #endregion

                var store = _unitOfWork.Repository<Store>().GetAll()

                  .Include(x => x.Campus)
                  .ThenInclude(x => x.TimeSlots)
                 .Where(x => x.Campus.TimeSlots.Any(x => x.Id == timeslotId))

                 .ProjectTo<StoreResponse>(_mapper.ConfigurationProvider)
                 .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

                return new BaseResponsePagingViewModel<StoreResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Page = paging.Page,
                        Size = paging.PageSize,
                        Total = store.Item1
                    },
                    Data = store.Item2.ToList()
                };
            }
            catch (ErrorResponse ex)
            {
                throw;
            }
        }
    }
}
