using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Commons;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using Microsoft.EntityFrameworkCore;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IRoomService
    {
        Task<BaseResponsePagingViewModel<RoomResponse>> GetRooms(RoomResponse filter, PagingRequest paging);
        Task<BaseResponsePagingViewModel<RoomResponse>> GetRoomsByFloorAndArea(int floorId, int areaId, PagingRequest paging);
    }
    public class RoomService : IRoomService
    {
        private IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public RoomService(IMapper mapper, IUnitOfWork unitOfWork)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponsePagingViewModel<RoomResponse>> GetRooms(RoomResponse filter, PagingRequest paging)
        {
            try
            {
                var room = _unitOfWork.Repository<Room>().GetAll()
                                        .ProjectTo<RoomResponse>(_mapper.ConfigurationProvider)
                                        .DynamicFilter(filter)
                                        .DynamicSort(filter)
                                        .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging,
                                        Constants.DefaultPaging);

                return new BaseResponsePagingViewModel<RoomResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Page = paging.Page,
                        Size = paging.PageSize,
                        Total = room.Item1
                    },
                    Data = room.Item2.ToList()
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }      

        public async Task<BaseResponsePagingViewModel<RoomResponse>> GetRoomsByFloorAndArea(int floorId, int areaId, PagingRequest paging)
        {
            try
            {
                #region check floor and area exist
                var checkFloor = _unitOfWork.Repository<Floor>().GetAll()
                              .FirstOrDefault(x => x.Id == floorId);
                if (checkFloor == null)
                    throw new ErrorResponse(404, (int)FloorErrorEnums.NOT_FOUND_ID,
                        FloorErrorEnums.NOT_FOUND_ID.GetDisplayName());

                var checkArea = _unitOfWork.Repository<Area>().GetAll()
                              .FirstOrDefault(x => x.Id == areaId);
                if (checkFloor == null)
                    throw new ErrorResponse(404, (int)AreaErrorEnums.NOT_FOUND_ID,
                        AreaErrorEnums.NOT_FOUND_ID.GetDisplayName());
                #endregion

                var room = _unitOfWork.Repository<Room>().GetAll()

                 .Where(x => x.FloorId == floorId && x.AreaId == areaId)

                 .ProjectTo<RoomResponse>(_mapper.ConfigurationProvider)
                 .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

                return new BaseResponsePagingViewModel<RoomResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Page = paging.Page,
                        Size = paging.PageSize,
                        Total = room.Item1
                    },
                    Data = room.Item2.ToList()
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
