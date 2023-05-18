using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Commons;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.ProductCollection;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Helpers;
using FINE.Service.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using static FINE.Service.Helpers.Enum;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IProductCollectionService
    {
        Task<BaseResponsePagingViewModel<ProductCollectionResponse>> GetAllProductCollection(ProductCollectionResponse request, PagingRequest paging);
        Task<BaseResponseViewModel<ProductCollectionResponse>> GetProductCollectionById(int productCollectionId);
        Task<BaseResponsePagingViewModel<ProductCollectionResponse>> GetProductCollectionByStore(int storeId, PagingRequest paging);
        Task<BaseResponseViewModel<ProductCollectionResponse>> CreateProductCollection(CreateProductCollectionRequest request);
        Task<BaseResponseViewModel<ProductCollectionResponse>> UpdateProductCollection(int productCollectionId, UpdateProductCollectionRequest request);
    }

    public class ProductCollectionService : IProductCollectionService
    {
        private IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        public ProductCollectionService(IMapper mapper, IUnitOfWork unitOfWork)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponseViewModel<ProductCollectionResponse>> CreateProductCollection(CreateProductCollectionRequest request)
        {
            
      
            var collection = _mapper.Map<CreateProductCollectionRequest, ProductCollection>(request);
            var checkStoreId = _unitOfWork.Repository<Store>().Find(x => x.Id == collection.StoreId );
            if(checkStoreId == null)
                throw new ErrorResponse(404, (int)ProductCollectionErrorEnums.NOT_FOUND_ID,
                    ProductCollectionErrorEnums.NOT_FOUND_ID.GetDisplayName());
            
            collection.Active = true;
            collection.CreateAt = DateTime.Now;

            await _unitOfWork.Repository<ProductCollection>().InsertAsync(collection);
            await _unitOfWork.CommitAsync();

            return new BaseResponseViewModel<ProductCollectionResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                }
            };
        }

        public async Task<BaseResponseViewModel<ProductCollectionResponse>> GetProductCollectionById(int productCollectionId)
        {
            var collection = _unitOfWork.Repository<ProductCollection>().GetAll()
                          .FirstOrDefault(x => x.Id == productCollectionId);

            if (collection == null)
                throw new ErrorResponse(404, (int)ProductCollectionErrorEnums.NOT_FOUND_ID,
                     ProductCollectionErrorEnums.NOT_FOUND_ID.GetDisplayName());

            return new BaseResponseViewModel<ProductCollectionResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<ProductCollectionResponse>(collection)
            };
        }

        public async Task<BaseResponsePagingViewModel<ProductCollectionResponse>> GetProductCollectionByStore(int storeId, PagingRequest paging)
        {
            var collection = _unitOfWork.Repository<ProductCollection>().GetAll()
                .Where(x => x.StoreId == storeId)
                .ProjectTo<ProductCollectionResponse>(_mapper.ConfigurationProvider)
                .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);
            
            var store = _unitOfWork.Repository<Store>().GetAll()
                          .FirstOrDefault(x => x.Id == storeId);

            if (store == null)
                throw new ErrorResponse(404, (int)StoreErrorEnums.NOT_FOUND_ID,
                    StoreErrorEnums.NOT_FOUND_ID.GetDisplayName());

            return new BaseResponsePagingViewModel<ProductCollectionResponse>()
            {
                Metadata = new PagingsMetadata()
                {
                    Page = paging.Page,
                    Size = paging.PageSize,
                    Total = collection.Item1
                },
                Data = collection.Item2.ToList()
            };
        }

        public async Task<BaseResponsePagingViewModel<ProductCollectionResponse>> GetAllProductCollection(ProductCollectionResponse filter, PagingRequest paging)
        {
            var collection = _unitOfWork.Repository<ProductCollection>().GetAll()

                .Include(x => x.ProductionCollectionItems)

                .Include(x=> x.ProductionCollectionItems)

                .ThenInclude(x => x.Product)
                .ProjectTo<ProductCollectionResponse>(_mapper.ConfigurationProvider)
                .DynamicFilter(filter)
            .DynamicSort(filter)
            .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging,
            Constants.DefaultPaging);

            return new BaseResponsePagingViewModel<ProductCollectionResponse>()
            {
                Metadata = new PagingsMetadata()
                {
                    Page = paging.Page,
                    Size = paging.PageSize,
                    Total = collection.Item1
                },
                Data = collection.Item2.ToList()
            };
        }

        public async Task<BaseResponseViewModel<ProductCollectionResponse>> UpdateProductCollection(int collectionId, UpdateProductCollectionRequest request)
        {
            var collection = _unitOfWork.Repository<ProductCollection>().GetAll()
                .FirstOrDefault(x => x.Id == collectionId);

            if (collection == null)
                throw new ErrorResponse(404, (int)ProductCollectionErrorEnums.NOT_FOUND_ID,
                    ProductCollectionErrorEnums.NOT_FOUND_ID.GetDisplayName());

            var updateCollection = _mapper.Map<UpdateProductCollectionRequest, ProductCollection>(request, collection);

            updateCollection.UpdateAt = DateTime.Now;

            await _unitOfWork.Repository<ProductCollection>().UpdateDetached(updateCollection);
            await _unitOfWork.CommitAsync();
          
            return new BaseResponseViewModel<ProductCollectionResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                }
            };
        }

      
    }
}
