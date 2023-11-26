using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Attributes;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Algorithm;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IFloorService
    {
        Task<BaseResponsePagingViewModel<FloorResponse>> GetFloorsByDestination(string destinationId, PagingRequest paging);
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



        public async Task<BaseResponsePagingViewModel<FloorResponse>> GetFloorsByDestination(string destinationId, PagingRequest paging)
        {
            try
            {
                #region check floor and area exis
                var destinationGuid = Guid.Parse(destinationId);
                var checkDestination = _unitOfWork.Repository<Destination>().GetAll()
                              .FirstOrDefault(x => x.Id == destinationGuid);
                if (checkDestination == null)
                    throw new ErrorResponse(404, (int)DestinationErrorEnums.NOT_FOUND,
                        DestinationErrorEnums.NOT_FOUND.GetDisplayName());
                #endregion

                var floor = _unitOfWork.Repository<Floor>().GetAll()
                                        .Where(x => x.DestionationId == destinationGuid)

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
