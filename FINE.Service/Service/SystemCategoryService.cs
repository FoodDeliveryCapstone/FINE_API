using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Commons;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Area;
using FINE.Service.DTO.Request.Campus;
using FINE.Service.DTO.Request.SystemCategory;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface ISystemCategoryService
    {
        Task<BaseResponsePagingViewModel<SystemCategoryResponse>> GetAllSystemCategory(SystemCategoryResponse filter, PagingRequest paging);
        Task<BaseResponseViewModel<SystemCategoryResponse>> GetSystemCategoryById(int id);
        Task<BaseResponseViewModel<SystemCategoryResponse>> GetSystemCategoryByCode(string code);
        Task<BaseResponseViewModel<SystemCategoryResponse>> CreateSystemCategory(CreateSystemCategoryRequest request);
        Task<BaseResponseViewModel<SystemCategoryResponse>> UpdateSystemCategory(int id, UpdateSystemCategoryRequest request);
    }

    public class SystemCategoryService : ISystemCategoryService
    {
        private IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        public SystemCategoryService(IMapper mapper, IUnitOfWork unitOfWork)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponsePagingViewModel<SystemCategoryResponse>> GetAllSystemCategory(SystemCategoryResponse filter, PagingRequest paging)
        {
            var systemCategory = _unitOfWork.Repository<SystemCategory>().GetAll()
                                    .ProjectTo<SystemCategoryResponse>(_mapper.ConfigurationProvider)
                                    .DynamicFilter(filter)
                                    .DynamicSort(filter)
                                    .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

            return new BaseResponsePagingViewModel<SystemCategoryResponse>()
            {
                Metadata = new PagingsMetadata()
                {
                    Page = paging.Page,
                    Size = paging.PageSize,
                    Total = systemCategory.Item1
                },
                Data = systemCategory.Item2.ToList()
            };
        }

        public async Task<BaseResponseViewModel<SystemCategoryResponse>> GetSystemCategoryById(int id)
        {
            var systemCategory = _unitOfWork.Repository<SystemCategory>().GetAll()
                         .FirstOrDefault(x => x.Id == id);

            if (systemCategory == null)
                throw new ErrorResponse(404, (int)SystemCategoryErrorEnums.NOT_FOUND_ID,
                    SystemCategoryErrorEnums.NOT_FOUND_ID.GetDisplayName());

            return new BaseResponseViewModel<SystemCategoryResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<SystemCategoryResponse>(systemCategory)
            };
        }

        public async Task<BaseResponseViewModel<SystemCategoryResponse>> GetSystemCategoryByCode(string code)
        {
            var systemCategory = _unitOfWork.Repository<SystemCategory>().GetAll()
                         .FirstOrDefault(x => x.CategoryCode == code);

            if (systemCategory == null)
                throw new ErrorResponse(404, (int)SystemCategoryErrorEnums.NOT_FOUND_CODE,
                    SystemCategoryErrorEnums.NOT_FOUND_CODE.GetDisplayName());

            return new BaseResponseViewModel<SystemCategoryResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<SystemCategoryResponse>(systemCategory)
            };
        }

        public async Task<BaseResponseViewModel<SystemCategoryResponse>> CreateSystemCategory(CreateSystemCategoryRequest request)
        {
            var checkCode = _unitOfWork.Repository<SystemCategory>().Find(x => x.CategoryCode == request.CategoryCode);
            if (checkCode != null)
            {
                throw new ErrorResponse(400, (int)SystemCategoryErrorEnums.CODE_EXSIST,
                                    SystemCategoryErrorEnums.CODE_EXSIST.GetDisplayName());
            }
            var systemCategory = _mapper.Map<CreateSystemCategoryRequest, SystemCategory>(request);
            systemCategory.CreateAt = DateTime.Now;

            await _unitOfWork.Repository<SystemCategory>().InsertAsync(systemCategory);
            await _unitOfWork.CommitAsync();

            return new BaseResponseViewModel<SystemCategoryResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<SystemCategoryResponse>(systemCategory)
            };
        }

        public async Task<BaseResponseViewModel<SystemCategoryResponse>> UpdateSystemCategory(int id, UpdateSystemCategoryRequest request)
        {
            var systemCategory = _unitOfWork.Repository<SystemCategory>()
                .Find(c => c.Id == id);

            if (systemCategory == null)
                throw new ErrorResponse(404, (int)SystemCategoryErrorEnums.NOT_FOUND_ID,
                    SystemCategoryErrorEnums.NOT_FOUND_ID.GetDisplayName());

            var updateSystemCategory = _mapper.Map<UpdateSystemCategoryRequest, SystemCategory>(request, systemCategory);
            updateSystemCategory.UpdateAt = DateTime.Now;

            await _unitOfWork.Repository<SystemCategory>().UpdateDetached(updateSystemCategory);
            await _unitOfWork.CommitAsync();

            return new BaseResponseViewModel<SystemCategoryResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<SystemCategoryResponse>(systemCategory)
            };
        }
    }
}
