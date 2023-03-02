using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Commons;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Campus;
using FINE.Service.DTO.Request.Store_Category;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using NTQ.Sdk.Core.Utilities;
using static FINE.Service.Helpers.ErrorEnum;


namespace FINE.Service.Service
{
    public interface IStoreCategoryService
    {
        Task<BaseResponsePagingViewModel<StoreCategoryResponse>> GetAllStoreCategory(StoreCategoryResponse filter, PagingRequest paging);
        Task<BaseResponseViewModel<StoreCategoryResponse>> GetStoreCategoryById(int id);
        Task<BaseResponsePagingViewModel<StoreCategoryResponse>> GetStoreCategoryByStore(int storeId, PagingRequest paging);
        Task<BaseResponseViewModel<StoreCategoryResponse>> CreateStoreCategory(CreateStoreCategoryRequest request);
        Task<BaseResponseViewModel<StoreCategoryResponse>> UpdateStoreCategory(int id, UpdateStoreCategoryRequest request);
    }

    public class StoreCategoryService : IStoreCategoryService
    {
        private IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        public StoreCategoryService(IMapper mapper, IUnitOfWork unitOfWork)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponsePagingViewModel<StoreCategoryResponse>> GetAllStoreCategory(StoreCategoryResponse filter, PagingRequest paging)
        {
            var storeCategory = _unitOfWork.Repository<StoreCategory>().GetAll()
               .ProjectTo<StoreCategoryResponse>(_mapper.ConfigurationProvider)
               .DynamicFilter(filter)
           .DynamicSort(filter)
           .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging,
           Constants.DefaultPaging);

            return new BaseResponsePagingViewModel<StoreCategoryResponse>()
            {
                Metadata = new PagingsMetadata()
                {
                    Page = paging.Page,
                    Size = paging.PageSize,
                    Total = storeCategory.Item1
                },
                Data = storeCategory.Item2.ToList()
            };
        }

        public async Task<BaseResponseViewModel<StoreCategoryResponse>> GetStoreCategoryById(int id)
        {
            var storeCategory = _unitOfWork.Repository<StoreCategory>().GetAll()
                          .FirstOrDefault(x => x.Id == id);

            if (storeCategory == null)
                throw new ErrorResponse(404, (int)StoreCategoryErrorEnums.NOT_FOUND_ID,
                    StoreCategoryErrorEnums.NOT_FOUND_ID.GetDisplayName());

            return new BaseResponseViewModel<StoreCategoryResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<StoreCategoryResponse>(storeCategory)
            };
        }

        public async Task<BaseResponsePagingViewModel<StoreCategoryResponse>> GetStoreCategoryByStore(int storeId, PagingRequest paging)
        {
            var storeCategory = _unitOfWork.Repository<StoreCategory>().GetAll()
                .Where(x => x.StoreId == storeId)
                .ProjectTo<StoreCategoryResponse>(_mapper.ConfigurationProvider)
                .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

            var store = _unitOfWork.Repository<Store>().GetAll()
                         .FirstOrDefault(x => x.Id == storeId);
            if (store == null)
            {
                throw new ErrorResponse(404, (int)StoreErrorEnums.NOT_FOUND_ID,
                    StoreErrorEnums.NOT_FOUND_ID.GetDisplayName());
            }
            return new BaseResponsePagingViewModel<StoreCategoryResponse>()
            {
                Metadata = new PagingsMetadata()
                {
                    Page = paging.Page,
                    Size = paging.PageSize,
                    Total = storeCategory.Item1
                },
                Data = storeCategory.Item2.ToList()
            };
        }

        public async Task<BaseResponseViewModel<StoreCategoryResponse>> CreateStoreCategory(CreateStoreCategoryRequest request)
        {
            var storeCategory = _mapper.Map<CreateStoreCategoryRequest, StoreCategory>(request);

            storeCategory.CreatedAt = DateTime.Now;

            await _unitOfWork.Repository<StoreCategory>().InsertAsync(storeCategory);
            await _unitOfWork.CommitAsync();

            return new BaseResponseViewModel<StoreCategoryResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                }
            };
        }

        public async Task<BaseResponseViewModel<StoreCategoryResponse>> UpdateStoreCategory(int id, UpdateStoreCategoryRequest request)
        {
            var storeCategory = _unitOfWork.Repository<StoreCategory>()
                 .Find(c => c.Id == id);

            if (storeCategory == null)
                throw new ErrorResponse(404, (int)StoreCategoryErrorEnums.NOT_FOUND_ID,
                    StoreCategoryErrorEnums.NOT_FOUND_ID.GetDisplayName());

            var updateStoreCategory = _mapper.Map<UpdateStoreCategoryRequest, StoreCategory>(request, storeCategory);
            updateStoreCategory.UpdatedAt = DateTime.Now;

            await _unitOfWork.Repository<StoreCategory>().UpdateDetached(updateStoreCategory);
            await _unitOfWork.CommitAsync();

            return new BaseResponseViewModel<StoreCategoryResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<StoreCategoryResponse>(storeCategory)
            };
        }
    }
}
