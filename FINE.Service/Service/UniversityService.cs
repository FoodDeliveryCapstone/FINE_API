using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Commons;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.University;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using NetTopologySuite.Algorithm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IUniversityService
    {
        Task<BaseResponsePagingViewModel<UniversityResponse>> GetUniversities(UniversityResponse filter, PagingRequest paging);
        Task<BaseResponseViewModel<UniversityResponse>> GetUniversityById(int universityId);
        Task<BaseResponseViewModel<UniversityResponse>> CreateUniversity(CreateUniversityRequest request);
        Task<BaseResponseViewModel<UniversityResponse>> UpdateUnivesity(int universityId, UpdateUniversityRequest request);
    }

    public class UniversityService : IUniversityService
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public UniversityService(IMapper mapper, IUnitOfWork unitOfWork)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponseViewModel<UniversityResponse>> CreateUniversity(CreateUniversityRequest request)
        {
            var universityCheck = _unitOfWork.Repository<University>().Find(x => x.UniCode == request.UniCode);
            if(universityCheck != null)
                throw new ErrorResponse(400, (int)UniversityErrorenums.CODE_EXSIST,
                                    UniversityErrorenums.CODE_EXSIST.GetDisplayName());
            var university = _mapper.Map<CreateUniversityRequest, University>(request);
            university.IsActive = true;
            university.CreateAt = DateTime.Now;
            await _unitOfWork.Repository<University>().InsertAsync(university);
            await _unitOfWork.CommitAsync();
            return new BaseResponseViewModel<UniversityResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<UniversityResponse>(university)
            };
        }

        public async Task<BaseResponsePagingViewModel<UniversityResponse>> GetUniversities(UniversityResponse filter, PagingRequest paging)
        {
            var university = _unitOfWork.Repository<University>().GetAll()
                                        .ProjectTo<UniversityResponse>(_mapper.ConfigurationProvider)
                                        .DynamicFilter(filter)
                                        .DynamicSort(filter)
                                        .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging,
                                    Constants.DefaultPaging);
            return new BaseResponsePagingViewModel<UniversityResponse>()
            {
                Metadata = new PagingsMetadata()
                {
                    Page = paging.Page,
                    Size = paging.PageSize,
                    Total = university.Item1
                },
                Data = university.Item2.ToList()
            };
        }

        public async Task<BaseResponseViewModel<UniversityResponse>> GetUniversityById(int universityId)
        {
            var university = _unitOfWork.Repository<University>().GetAll()
                                        .FirstOrDefault(x => x.Id == universityId);
            if(university == null)
                throw new ErrorResponse(404, (int)UniversityErrorenums.NOT_FOUND_ID,
                                    UniversityErrorenums.NOT_FOUND_ID.GetDisplayName());
            return new BaseResponseViewModel<UniversityResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<UniversityResponse>(university)
            };
        }

        public async Task<BaseResponseViewModel<UniversityResponse>> UpdateUnivesity(int universityId, UpdateUniversityRequest request)
        {
            var university = _unitOfWork.Repository<University>().Find(x => x.Id == universityId);
            if(university == null)
                throw new ErrorResponse(404, (int)UniversityErrorenums.NOT_FOUND_ID,
                                    UniversityErrorenums.NOT_FOUND_ID.GetDisplayName());
            var universityMappingResult = _mapper.Map<UpdateUniversityRequest, University>(request, university);
            universityMappingResult.UpdateAt = DateTime.Now;
            await _unitOfWork.Repository<University>().UpdateDetached(universityMappingResult);
            await _unitOfWork.CommitAsync();
            return new BaseResponseViewModel<UniversityResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<UniversityResponse>(universityMappingResult)
            };
        }
    }
}
