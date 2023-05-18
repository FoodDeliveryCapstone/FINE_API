using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Commons;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Campus;
using FINE.Service.DTO.Request.Product_Collection_Item;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IProductCollectionItemService
    {
        Task<BaseResponsePagingViewModel<ProductCollectionItemResponse>> GetAllProductCollectionItem(ProductCollectionItemResponse filter, PagingRequest paging);
        Task<BaseResponseViewModel<ProductCollectionItemResponse>> GetProductCollectionItemById(int id);
        Task<BaseResponsePagingViewModel<ProductCollectionItemResponse>> GetProductCollectionItemByProductCollection(int productCollectionId, PagingRequest paging);
        Task<BaseResponsePagingViewModel<ProductCollectionItemResponse>> GetProductCollectionItemByProduct(int productId, PagingRequest paging);
        Task<BaseResponseViewModel<ProductCollectionItemResponse>> CreateProductCollectionItem(CreateProductCollectionItemRequest request);
        Task<BaseResponseViewModel<ProductCollectionItemResponse>> UpdateProductCollectionItem(int id, UpdateProductCollectionItemRequest request);
    }

    public class ProductCollectionItemService : IProductCollectionItemService
    {
        private IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        public ProductCollectionItemService(IMapper mapper, IUnitOfWork unitOfWork)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponseViewModel<ProductCollectionItemResponse>> CreateProductCollectionItem(CreateProductCollectionItemRequest request)
        {
            var productCollectionItem = _mapper.Map<CreateProductCollectionItemRequest, ProductionCollectionItem>(request);

            productCollectionItem.CreateAt = DateTime.Now;

            await _unitOfWork.Repository<ProductionCollectionItem>().InsertAsync(productCollectionItem);
            await _unitOfWork.CommitAsync();

            return new BaseResponseViewModel<ProductCollectionItemResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                }
            };
        }

        public async Task<BaseResponsePagingViewModel<ProductCollectionItemResponse>> GetAllProductCollectionItem(ProductCollectionItemResponse filter, PagingRequest paging)
        {
            var productCollectionItem = _unitOfWork.Repository<ProductionCollectionItem>().GetAll()
                                       .ProjectTo<ProductCollectionItemResponse>(_mapper.ConfigurationProvider)
                                       .DynamicFilter(filter)
                                       .DynamicSort(filter)
                                       .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

            return new BaseResponsePagingViewModel<ProductCollectionItemResponse>()
            {
                Metadata = new PagingsMetadata()
                {
                    Page = paging.Page,
                    Size = paging.PageSize,
                    Total = productCollectionItem.Item1
                },
                Data = productCollectionItem.Item2.ToList()
            };
        }

        public async Task<BaseResponseViewModel<ProductCollectionItemResponse>> GetProductCollectionItemById(int id)
        {
            var productCollectionItem = _unitOfWork.Repository<ProductionCollectionItem>().GetAll()
                         .FirstOrDefault(x => x.Id == id);

            if (productCollectionItem == null)
                throw new ErrorResponse(404, (int)ProductCollectionItemErrorEnums.NOT_FOUND_ID,
                    ProductCollectionItemErrorEnums.NOT_FOUND_ID.GetDisplayName());

            return new BaseResponseViewModel<ProductCollectionItemResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<ProductCollectionItemResponse>(productCollectionItem)
            };
        }

        public async Task<BaseResponsePagingViewModel<ProductCollectionItemResponse>> GetProductCollectionItemByProduct(int productId, PagingRequest paging)
        {
            var productCollectionItem = _unitOfWork.Repository<ProductionCollectionItem>().GetAll()
                                                   .Where(x => x.ProductId == productId)
                                                   .ProjectTo<ProductCollectionItemResponse>(_mapper.ConfigurationProvider)
                                                   .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

            var product = _unitOfWork.Repository<Product>().GetAll()
                        .FirstOrDefault(x => x.Id == productId);
            if (product == null)
                throw new ErrorResponse(404, (int)ProductErrorEnums.NOT_FOUND_ID,
                    ProductErrorEnums.NOT_FOUND_ID.GetDisplayName());

            return new BaseResponsePagingViewModel<ProductCollectionItemResponse>()
            {
                Metadata = new PagingsMetadata()
                {
                    Page = paging.Page,
                    Size = paging.PageSize,
                    Total = productCollectionItem.Item1
                },
                Data = productCollectionItem.Item2.ToList()
            };
        }

        public async Task<BaseResponsePagingViewModel<ProductCollectionItemResponse>> GetProductCollectionItemByProductCollection(int productCollectionId, PagingRequest paging)
        {
            var productCollectionItem = _unitOfWork.Repository<ProductionCollectionItem>().GetAll()
                                                   .Where(x => x.ProductCollectionId == productCollectionId)
                                                   .ProjectTo<ProductCollectionItemResponse>(_mapper.ConfigurationProvider)
                                                   .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

            var productCollection = _unitOfWork.Repository<Product>().GetAll()
                        .FirstOrDefault(x => x.Id == productCollectionId);
            if (productCollection == null)
                throw new ErrorResponse(404, (int)ProductCollectionErrorEnums.NOT_FOUND_ID,
                    ProductCollectionErrorEnums.NOT_FOUND_ID.GetDisplayName());

            return new BaseResponsePagingViewModel<ProductCollectionItemResponse>()
            {
                Metadata = new PagingsMetadata()
                {
                    Page = paging.Page,
                    Size = paging.PageSize,
                    Total = productCollectionItem.Item1
                },
                Data = productCollectionItem.Item2.ToList()
            };
        }

        public async Task<BaseResponseViewModel<ProductCollectionItemResponse>> UpdateProductCollectionItem(int id, UpdateProductCollectionItemRequest request)
        {
            var productCollectionItem = _unitOfWork.Repository<ProductionCollectionItem>()
                 .Find(c => c.Id == id);

            if (productCollectionItem == null)
                throw new ErrorResponse(404, (int)ProductCollectionItemErrorEnums.NOT_FOUND_ID,
                    ProductCollectionItemErrorEnums.NOT_FOUND_ID.GetDisplayName());

            var updateProductCollectionItem = _mapper.Map<UpdateProductCollectionItemRequest, ProductionCollectionItem>(request, productCollectionItem);
            updateProductCollectionItem.UpdateAt = DateTime.Now;

            await _unitOfWork.Repository<ProductionCollectionItem>().UpdateDetached(updateProductCollectionItem);
            await _unitOfWork.CommitAsync();

            return new BaseResponseViewModel<ProductCollectionItemResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<ProductCollectionItemResponse>(productCollectionItem)
            };
        }
    }
}
