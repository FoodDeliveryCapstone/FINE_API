using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Commons;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Algorithm;
using NTQ.Sdk.Core.Utilities;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IFloorService
    {
        Task<BaseResponsePagingViewModel<FloorResponse>> GetFloorsByCampus(int campusId, PagingRequest paging);
    }

    public class FloorService : IFloorService
    {
        private IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public FloorService(IMapper mapper, IUnitOfWork unitOfWork)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

  

        public async Task<BaseResponsePagingViewModel<FloorResponse>> GetFloorsByCampus(int campusId, PagingRequest paging)
        {
            try
            {
                #region check floor and area exist
                var checkCampus = _unitOfWork.Repository<Floor>().GetAll()
                              .FirstOrDefault(x => x.Id == campusId);
                if (checkCampus == null)
                    throw new ErrorResponse(404, (int)CampusErrorEnums.NOT_FOUND_ID,
                        CampusErrorEnums.NOT_FOUND_ID.GetDisplayName());
                #endregion

                var floor = _unitOfWork.Repository<Floor>().GetAll()

                 .Where(x => x.CampusId == campusId)

                 .ProjectTo<FloorResponse>(_mapper.ConfigurationProvider)
                 .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

                return new BaseResponsePagingViewModel<FloorResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Page = paging.Page,
                        Size = paging.PageSize,
                        Total = floor.Item1
                    },
                    Data = floor.Item2.ToList()
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
