using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Attributes;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Area;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using Microsoft.EntityFrameworkCore;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IAreaService
    {
        Task<BaseResponsePagingViewModel<AreaResponse>> GetAreas(AreaResponse filter, PagingRequest paging);
        Task<BaseResponseViewModel<AreaResponse>> GetAreaById(int areaId);
        Task<BaseResponseViewModel<AreaResponse>> CreateArea(CreateAreaRequest request);
        Task<BaseResponseViewModel<AreaResponse>> UpdateArea(int areaId, UpdateAreaRequest request);
        Task<BaseResponsePagingViewModel<AreaResponse>> GetAreaByCampusId(int campusId, PagingRequest paging);

    }

    public class AreaService : IAreaService
    {
        private IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public AreaService(IMapper mapper, IUnitOfWork unitOfWork)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponseViewModel<AreaResponse>> CreateArea(CreateAreaRequest request)
        {
            try
            {
                var checkArea = _unitOfWork.Repository<Area>().Find(x => x.AreaCode.Contains(request.AreaCode));

                if (checkArea != null)
                {
                    throw new ErrorResponse(400, (int)AreaErrorEnums.CODE_EXSIST,
                                        AreaErrorEnums.CODE_EXSIST.GetDisplayName());
                }
                var area = _mapper.Map<CreateAreaRequest, Area>(request);
                area.CreateAt = DateTime.Now;

                await _unitOfWork.Repository<Area>().InsertAsync(area);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<AreaResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<AreaResponse>(area)
                };
            }
            catch (ErrorResponse ex)
            {
                throw;
            }
        }

        public async Task<BaseResponseViewModel<AreaResponse>> GetAreaById(int AreaId)
        {
            try
            {
                var area = _unitOfWork.Repository<Area>().GetAll()
                                            .FirstOrDefault(x => x.Id == AreaId);
                if (area == null)
                    throw new ErrorResponse(404, (int)AreaErrorEnums.NOT_FOUND_ID,
                                        AreaErrorEnums.NOT_FOUND_ID.GetDisplayName());

                return new BaseResponseViewModel<AreaResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<AreaResponse>(area)
                };
            }
            catch (ErrorResponse ex)
            {
                throw;
            }
        }

        public async Task<BaseResponsePagingViewModel<AreaResponse>> GetAreas(AreaResponse filter, PagingRequest paging)
        {
            try
            {
                var areas = _unitOfWork.Repository<Area>().GetAll()
                                    .Include(x => x.Rooms)
                                    .ProjectTo<AreaResponse>(_mapper.ConfigurationProvider)
                                    .DynamicFilter<AreaResponse>(filter)
                                    .PagingQueryable(paging.Page, paging.PageSize,
                               Constants.LimitPaging, Constants.DefaultPaging);

                return new BaseResponsePagingViewModel<AreaResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Page = paging.Page,
                        Size = paging.PageSize,
                        Total = areas.Item1
                    },
                    Data = areas.Item2.ToList()
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<BaseResponseViewModel<AreaResponse>> UpdateArea(int areaId, UpdateAreaRequest request)
        {
            try
            {
                Area area = _unitOfWork.Repository<Area>().Find(x => x.Id == areaId);

                if (area == null)
                    throw new ErrorResponse(404, (int)AreaErrorEnums.NOT_FOUND_ID,
                                        AreaErrorEnums.NOT_FOUND_ID.GetDisplayName());

                var updateArea = _mapper.Map<UpdateAreaRequest, Area>(request, area);

                updateArea.UpdateAt = DateTime.Now;

                await _unitOfWork.Repository<Area>().UpdateDetached(updateArea);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<AreaResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<AreaResponse>(updateArea)
                };
            }
            catch (ErrorResponse ex)
            {
                throw;
            }
        }

        public async Task<BaseResponsePagingViewModel<AreaResponse>> GetAreaByCampusId(int campusId, PagingRequest paging)
        {
            try
            {
                var checkCampus = _unitOfWork.Repository<Campus>().GetAll()
                                             .FirstOrDefault(x => x.Id == campusId);

                if (checkCampus == null)
                    throw new ErrorResponse(404, (int)CampusErrorEnums.NOT_FOUND_ID,
                                                    CampusErrorEnums.NOT_FOUND_ID.GetDisplayName());

                var area = _unitOfWork.Repository<Area>().GetAll()
                                        .Where(x => x.CampusId == campusId)
                                        .ProjectTo<AreaResponse>(_mapper.ConfigurationProvider)
                                        .PagingQueryable(paging.Page, paging.PageSize,
                                        Constants.LimitPaging, Constants.DefaultPaging);

                return new BaseResponsePagingViewModel<AreaResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Page = paging.Page,
                        Size = paging.PageSize,
                        Total = area.Item1
                    },
                    Data = area.Item2.ToList()
                };
            }
            catch (ErrorResponse ex)
            {
                throw;
            }
        }
    }
}
