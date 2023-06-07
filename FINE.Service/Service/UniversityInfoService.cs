using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Attributes;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.UniversityInfo;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IUniversityInfoService
    {
        Task<BaseResponsePagingViewModel<UniversityInfoResponse>> GetUniversityInformations(UniversityInfoResponse filter, PagingRequest paging);
        Task<BaseResponseViewModel<UniversityInfoResponse>> GetUniversityInfoById(int universityInfoId);
        Task<BaseResponseViewModel<UniversityInfoResponse>> CreateUniversityInfo(CreateUniversityInfoRequest request);
        Task<BaseResponseViewModel<UniversityInfoResponse>> UpdateUniversityInfo(int universityInfoId, UpdateUniversityInfoRequest request);
    }

    public class UniversityInfoService : IUniversityInfoService
    {
        private IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public UniversityInfoService(IMapper mapepr, IUnitOfWork unitOfWork)
        {
            _mapper = mapepr;
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponseViewModel<UniversityInfoResponse>> CreateUniversityInfo(CreateUniversityInfoRequest request)
        {
            var universityInfo = _mapper.Map<CreateUniversityInfoRequest, UniversityInfo>(request);
            universityInfo.CreateAt = DateTime.Now;
            await _unitOfWork.Repository<UniversityInfo>().InsertAsync(universityInfo);
            await _unitOfWork.CommitAsync();
            return new BaseResponseViewModel<UniversityInfoResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<UniversityInfoResponse>(universityInfo)
            };
        }

        public async Task<BaseResponseViewModel<UniversityInfoResponse>> GetUniversityInfoById(int universityInfoId)
        {
            var universityInfo = _unitOfWork.Repository<UniversityInfo>().GetAll()
                                            .FirstOrDefault(x => x.Id == universityInfoId);
            return new BaseResponseViewModel<UniversityInfoResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<UniversityInfoResponse>(universityInfo)
            };
        }

        public async Task<BaseResponsePagingViewModel<UniversityInfoResponse>> GetUniversityInformations(UniversityInfoResponse filter, PagingRequest paging)
        {
            var universityInfo = _unitOfWork.Repository<UniversityInfo>().GetAll()
                                            .ProjectTo<UniversityInfoResponse>(_mapper.ConfigurationProvider)
                                            .DynamicFilter(filter)
                                            .DynamicSort(filter)
                                            .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging,
                                    Constants.DefaultPaging);
            return new BaseResponsePagingViewModel<UniversityInfoResponse>()
            {
                Metadata = new PagingsMetadata()
                {
                    Page = paging.Page,
                    Size = paging.PageSize,
                    Total = universityInfo.Item1
                },
                Data = universityInfo.Item2.ToList()
            };
        }

        public async Task<BaseResponseViewModel<UniversityInfoResponse>> UpdateUniversityInfo(int universityInfoId, UpdateUniversityInfoRequest request)
        {
            var universityInfo = _unitOfWork.Repository<UniversityInfo>().Find(x => x.Id == universityInfoId);
            if (universityInfo == null)
            {
                throw new ErrorResponse(404, (int)UnversityInfoErrorEnums.NOT_FOUND_ID,
                                   UnversityInfoErrorEnums.NOT_FOUND_ID.GetDisplayName());
            }
            var universityInfoMappingResult = _mapper.Map<UpdateUniversityInfoRequest, UniversityInfo>(request, universityInfo);
            universityInfoMappingResult.UpdateAt = DateTime.Now;
            await _unitOfWork.Repository<UniversityInfo>().UpdateDetached(universityInfoMappingResult);
            await _unitOfWork.CommitAsync();
            return new BaseResponseViewModel<UniversityInfoResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<UniversityInfoResponse>(universityInfoMappingResult)
            };
        }
    }
}
