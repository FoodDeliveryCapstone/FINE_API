using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Commons;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Store;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using NTQ.Sdk.Core.Utilities;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IStoreService
    {
        Task<BaseResponsePagingViewModel<StoreResponse>> GetStores(StoreResponse filter, PagingRequest paging);
        Task<BaseResponseViewModel<StoreResponse>> GetStoreById(int storeId);
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

        public async Task<BaseResponseViewModel<StoreResponse>> GetStoreById(int storeId)
        {
            var CheckStore = _unitOfWork.Repository<Store>().GetAll()
                                        .FirstOrDefault(x => x.Id == storeId);
            if (CheckStore == null)
                throw new ErrorResponse(404, (int)StoreErrorEnums.NOT_FOUND_CODE,
                                    StoreErrorEnums.NOT_FOUND_CODE.GetDisplayName());
            return new BaseResponseViewModel<StoreResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<StoreResponse>(CheckStore)
            };
        }

        public async Task<BaseResponsePagingViewModel<StoreResponse>> GetStores(StoreResponse filter, PagingRequest paging)
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

        public async Task<BaseResponseViewModel<StoreResponse>> UpdateStore(int storeId, UpdateStoreRequest request)
        {
            Store store = _unitOfWork.Repository<Store>().Find(x => x.Id == storeId);

            if (store == null)
                throw new ErrorResponse(404, (int)StoreErrorEnums.NOT_FOUND_CODE,
                                    StoreErrorEnums.NOT_FOUND_CODE.GetDisplayName());

            var storeMapping = _mapper.Map<UpdateStoreRequest, Store>(request, store);
            if (storeMapping.CampusId == null) storeMapping.CampusId = store.CampusId;
            else if (storeMapping.StoreName == null) storeMapping.StoreName = store.StoreName;
            else if (storeMapping.ImageUrl == null) storeMapping.ImageUrl = store.ImageUrl;
            else if (storeMapping.ContactPerson == null) storeMapping.ContactPerson = store.ContactPerson;
            else if (storeMapping.IsActive == null) storeMapping.IsActive = store.IsActive;
            storeMapping.UpdatedAt = DateTime.Now;
            await _unitOfWork.Repository<Store>().UpdateDetached(storeMapping);
            await _unitOfWork.CommitAsync();
            return new BaseResponseViewModel<StoreResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<StoreResponse>(storeMapping)
            };
        }
    }
}
