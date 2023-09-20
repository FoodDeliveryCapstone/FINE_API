using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Attributes;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IStationService
    {
        Task<BaseResponsePagingViewModel<StationResponse>> GetStationByDestination(string destinationId, PagingRequest paging);
        Task<BaseResponseViewModel<StationResponse>> GetStationById(string stationId);
    }

    public class StationService : IStationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public StationService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponsePagingViewModel<StationResponse>> GetStationByDestination(string destinationId, PagingRequest paging)
        {
            try
            {
                var checkDestination = _unitOfWork.Repository<Destination>().GetAll().Any(x => x.Id == Guid.Parse(destinationId));
                if (checkDestination == false)
                    throw new ErrorResponse(404, (int)StationErrorEnums.NOT_FOUND,
                       StationErrorEnums.NOT_FOUND.GetDisplayName());

                var stations = _unitOfWork.Repository<Station>().GetAll()
                                .Where(x => x.Floor.DestionationId == Guid.Parse(destinationId) && x.IsActive == true)
                                .ProjectTo<StationResponse>(_mapper.ConfigurationProvider)
                                .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

                return new BaseResponsePagingViewModel<StationResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Page = paging.Page,
                        Size = paging.PageSize,
                        Total = stations.Item1
                    },
                    Data = stations.Item2.ToList()
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<StationResponse>> GetStationById(string stationId)
        {
            try
            {
                var id = Guid.Parse(stationId);
                var station = _unitOfWork.Repository<Station>().GetAll()
                                .FirstOrDefault(x => x.Id == id);
                if (station == null)
                    throw new ErrorResponse(404, (int)StationErrorEnums.NOT_FOUND,
                       StationErrorEnums.NOT_FOUND.GetDisplayName());

                return new BaseResponseViewModel<StationResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<StationResponse>(station)
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}

