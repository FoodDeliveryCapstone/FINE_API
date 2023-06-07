using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Attributes;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Product_Collection_Time_Slot;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IProductCollectionTimeSlotService
    {
        Task<BaseResponsePagingViewModel<ProductCollectionTimeSlotResponse>> GetAllProductCollectionTimeSlot(ProductCollectionTimeSlotResponse filter, PagingRequest paging);
        Task<BaseResponseViewModel<ProductCollectionTimeSlotResponse>> GetProductCollectionTimeSlotById(int id);
        Task<BaseResponsePagingViewModel<ProductCollectionTimeSlotResponse>> GetProductCollectionTimeSlotByProductCollection(int productCollectionId, PagingRequest paging);
        Task<BaseResponsePagingViewModel<ProductCollectionTimeSlotResponse>> GetProductCollectionTimeSlotByTimeSlot(int timeSlotId, PagingRequest paging);
        Task<BaseResponseViewModel<ProductCollectionTimeSlotResponse>> CreateProductCollectionTimeSlot(CreateProductCollectionTimeSlotRequest request);
        Task<BaseResponseViewModel<ProductCollectionTimeSlotResponse>> UpdateProductCollectionTimeSlot(int id, UpdateProductCollectionTimeSlotRequest request);
    }

    public class ProductCollectionTimeSlotService : IProductCollectionTimeSlotService
    {
        private IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        public ProductCollectionTimeSlotService(IMapper mapper, IUnitOfWork unitOfWork)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponseViewModel<ProductCollectionTimeSlotResponse>> CreateProductCollectionTimeSlot(CreateProductCollectionTimeSlotRequest request)
        {
            var productCollectionTimeSLot = _mapper.Map<CreateProductCollectionTimeSlotRequest, ProductCollectionTimeSlot>(request);

            productCollectionTimeSLot.CreatedAt = DateTime.Now;

            await _unitOfWork.Repository<ProductCollectionTimeSlot>().InsertAsync(productCollectionTimeSLot);
            await _unitOfWork.CommitAsync();

            return new BaseResponseViewModel<ProductCollectionTimeSlotResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                }
            };
        }

        public async Task<BaseResponsePagingViewModel<ProductCollectionTimeSlotResponse>> GetAllProductCollectionTimeSlot(ProductCollectionTimeSlotResponse filter, PagingRequest paging)
        {
            var productCollectionTimeSlot = _unitOfWork.Repository<ProductCollectionTimeSlot>().GetAll()
                                                       .ProjectTo<ProductCollectionTimeSlotResponse>(_mapper.ConfigurationProvider)
                                                       .DynamicFilter(filter)
                                                       .DynamicSort(filter)
                                                       .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

            return new BaseResponsePagingViewModel<ProductCollectionTimeSlotResponse>()
            {
                Metadata = new PagingsMetadata()
                {
                    Page = paging.Page,
                    Size = paging.PageSize,
                    Total = productCollectionTimeSlot.Item1
                },
                Data = productCollectionTimeSlot.Item2.ToList()
            };
        }

        public async Task<BaseResponseViewModel<ProductCollectionTimeSlotResponse>> GetProductCollectionTimeSlotById(int id)
        {
            var productCollectionTimeSlot = _unitOfWork.Repository<ProductCollectionTimeSlot>().GetAll()
                          .FirstOrDefault(x => x.Id == id);

            if (productCollectionTimeSlot == null)
                throw new ErrorResponse(404, (int)ProductCollectionTimeSlotErrorEnums.NOT_FOUND_ID,
                    ProductCollectionTimeSlotErrorEnums.NOT_FOUND_ID.GetDisplayName());

            return new BaseResponseViewModel<ProductCollectionTimeSlotResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<ProductCollectionTimeSlotResponse>(productCollectionTimeSlot)
            };
        }

        public async Task<BaseResponsePagingViewModel<ProductCollectionTimeSlotResponse>> GetProductCollectionTimeSlotByProductCollection(int productCollectionId, PagingRequest paging)
        {
            var productCollectionTimeSlot = _unitOfWork.Repository<ProductCollectionTimeSlot>().GetAll()
                                                        .Where(x => x.ProductCollectionId == productCollectionId)
                                                        .ProjectTo<ProductCollectionTimeSlotResponse>(_mapper.ConfigurationProvider)
                                                        .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

            var productCollection = _unitOfWork.Repository<ProductCollection>().GetAll()
                        .FirstOrDefault(x => x.Id == productCollectionId);
            if (productCollection == null)
                throw new ErrorResponse(404, (int)ProductCollectionErrorEnums.NOT_FOUND_ID,
                    ProductCollectionErrorEnums.NOT_FOUND_ID.GetDisplayName());

            return new BaseResponsePagingViewModel<ProductCollectionTimeSlotResponse>()
            {
                Metadata = new PagingsMetadata()
                {
                    Page = paging.Page,
                    Size = paging.PageSize,
                    Total = productCollectionTimeSlot.Item1
                },
                Data = productCollectionTimeSlot.Item2.ToList()
            };
        }

        public async Task<BaseResponsePagingViewModel<ProductCollectionTimeSlotResponse>> GetProductCollectionTimeSlotByTimeSlot(int timeSlotId, PagingRequest paging)
        {
            var productCollectionTimeSlot = _unitOfWork.Repository<ProductCollectionTimeSlot>().GetAll()
                                                       .Where(x => x.TimeSlotId == timeSlotId)
                                                       .ProjectTo<ProductCollectionTimeSlotResponse>(_mapper.ConfigurationProvider)
                                                       .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

            var timeSlot = _unitOfWork.Repository<TimeSlot>().GetAll()
                        .FirstOrDefault(x => x.Id == timeSlotId);
            if (timeSlot == null)
                throw new ErrorResponse(404, (int)TimeSlotErrorEnums.NOT_FOUND,
                    TimeSlotErrorEnums.NOT_FOUND.GetDisplayName());

            return new BaseResponsePagingViewModel<ProductCollectionTimeSlotResponse>()
            {
                Metadata = new PagingsMetadata()
                {
                    Page = paging.Page,
                    Size = paging.PageSize,
                    Total = productCollectionTimeSlot.Item1
                },
                Data = productCollectionTimeSlot.Item2.ToList()
            };
        }

        public async Task<BaseResponseViewModel<ProductCollectionTimeSlotResponse>> UpdateProductCollectionTimeSlot(int id, UpdateProductCollectionTimeSlotRequest request)
        {
            var productCollectionTimeSlot = _unitOfWork.Repository<ProductCollectionTimeSlot>()
                .Find(c => c.Id == id);

            if (productCollectionTimeSlot == null)
                throw new ErrorResponse(404, (int)ProductCollectionTimeSlotErrorEnums.NOT_FOUND_ID,
                    ProductCollectionTimeSlotErrorEnums.NOT_FOUND_ID.GetDisplayName());

            var updateProductCollectionTimeSlot = _mapper.Map<UpdateProductCollectionTimeSlotRequest, ProductCollectionTimeSlot>(request, productCollectionTimeSlot);
            updateProductCollectionTimeSlot.UpdatedAt = DateTime.Now;

            await _unitOfWork.Repository<ProductCollectionTimeSlot>().UpdateDetached(updateProductCollectionTimeSlot);
            await _unitOfWork.CommitAsync();

            return new BaseResponseViewModel<ProductCollectionTimeSlotResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<ProductCollectionTimeSlotResponse>(productCollectionTimeSlot)
            };
        }
    }
}
