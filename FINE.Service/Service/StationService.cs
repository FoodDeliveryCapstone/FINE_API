using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Attributes;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Station;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Helpers;
using FINE.Service.Utilities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IStationService
    {
        Task<BaseResponsePagingViewModel<StationResponse>> GetStationByDestination(string destinationId, PagingRequest paging);
        Task<BaseResponseViewModel<List<StationResponse>>> GetStationByDestinationForOrder(string destinationId, int numberBox);
        Task<BaseResponseViewModel<StationResponse>> GetStationById(string stationId);
        Task<BaseResponseViewModel<StationResponse>> CreateStation(CreateStationRequest request);
        Task<BaseResponseViewModel<StationResponse>> UpdateStation(string stationId, UpdateStationRequest request);
        Task<BaseResponseViewModel<StationResponse>> LockBox(string stationId, string orderId, int numberBox);

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
                                .Where(x => x.Floor.DestionationId == Guid.Parse(destinationId) && x.IsActive == true && x.IsAvailable == true)
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

        public async Task<BaseResponseViewModel<List<StationResponse>>> GetStationByDestinationForOrder(string destinationId, int numberBox)
        {
            try
            {
                var checkDestination = _unitOfWork.Repository<Destination>().GetAll().Any(x => x.Id == Guid.Parse(destinationId));
                if (checkDestination == false)
                    throw new ErrorResponse(404, (int)StationErrorEnums.NOT_FOUND,
                       StationErrorEnums.NOT_FOUND.GetDisplayName());

                var listStation = _unitOfWork.Repository<Station>().GetAll()
                                .Where(x => x.Floor.DestionationId == Guid.Parse(destinationId)
                                           && x.IsActive == true
                                           && x.IsAvailable == true);

                var result = new List<StationResponse>();
                var key = RedisDbEnum.Box.GetDisplayName() + ":Station";

                List<LockBoxinStationModel> listStationLockBox = new List<LockBoxinStationModel>();
                var redisValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, key, null);
                if (redisValue.HasValue == true)
                {
                    listStationLockBox = JsonConvert.DeserializeObject<List<LockBoxinStationModel>>(redisValue);
                }
                else
                {
                    listStationLockBox = listStation.Select(x => new LockBoxinStationModel
                    {
                        StationName = x.Name,
                        StationId = x.Id,
                        NumberBoxLockPending = 0
                    }).ToList();
                }
                foreach (var stationLock in listStationLockBox)
                {
                    //get các station còn available kể cả box lock
                    var stationFit = await listStation.Where(x => x.Id == stationLock.StationId
                                                && x.Boxes.Where(x => x.IsActive == true
                                                && x.OrderBoxes.Any(y => y.Status != (int)OrderBoxStatusEnum.Picked) == false).Count()
                                                - stationLock.NumberBoxLockPending >= numberBox)
                                    .ProjectTo<StationResponse>(_mapper.ConfigurationProvider)
                                    .FirstOrDefaultAsync();
                    if (stationFit is not null)
                    {
                        stationLock.NumberBoxLockPending += numberBox;
                        result.Add(stationFit);
                    }
                }
                await ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, key, listStationLockBox);

                return new BaseResponseViewModel<List<StationResponse>>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = result
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<StationResponse>> LockBox(string stationId, string orderCode, int numberBox)
        {
            try
            {
                var station = await _unitOfWork.Repository<Station>().GetAll().FirstOrDefaultAsync(x => x.Id == Guid.Parse(stationId));
                var key = RedisDbEnum.Box.GetDisplayName() + ":Station:" + station.Code;


                ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, key, null);

                return new BaseResponseViewModel<StationResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    }
                };
            }
            catch (Exception ex)
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

        public async Task<BaseResponseViewModel<StationResponse>> CreateStation(CreateStationRequest request)
        {
            try
            {
                var checkCode = await _unitOfWork.Repository<Station>().GetAll().FirstOrDefaultAsync(x => x.Code == request.Code);
                if (checkCode != null)
                    throw new ErrorResponse(404, (int)StationErrorEnums.CODE_EXIST,
                       StationErrorEnums.CODE_EXIST.GetDisplayName());
                var checkFloor = await _unitOfWork.Repository<Floor>().GetAll().FirstOrDefaultAsync(x => x.Id == request.FloorId);
                if (checkFloor == null)
                    throw new ErrorResponse(404, (int)FloorErrorEnums.NOT_FOUND,
                       FloorErrorEnums.NOT_FOUND.GetDisplayName());

                //lấy ds area trong json
                string jsonFilePath = "Configuration\\listArea.json";
                string jsonString = File.ReadAllText(jsonFilePath);
                var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(jsonString);
                var allAreaCode = jsonObject["AreaCodes"];
                var areaCode = allAreaCode.FirstOrDefault(x => x == request.AreaCode);
                if (areaCode == null)
                    throw new ErrorResponse(404, (int)AreaErrorEnums.NOT_FOUND_CODE,
                       AreaErrorEnums.NOT_FOUND_CODE.GetDisplayName());

                var station = _mapper.Map<CreateStationRequest, Station>(request);

                station.Id = Guid.NewGuid();
                station.AreaCode = areaCode;
                station.IsAvailable = true;
                station.IsActive = true;
                station.CreateAt = DateTime.Now;

                await _unitOfWork.Repository<Station>().InsertAsync(station);
                await _unitOfWork.CommitAsync();

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

        public async Task<BaseResponseViewModel<StationResponse>> UpdateStation(string stationId, UpdateStationRequest request)
        {
            try
            {
                var getAllStation = _unitOfWork.Repository<Station>().GetAll();
                var station = getAllStation.FirstOrDefault(x => x.Id == Guid.Parse(stationId));
                if (station == null)
                    throw new ErrorResponse(404, (int)StationErrorEnums.NOT_FOUND,
                        StationErrorEnums.NOT_FOUND.GetDisplayName());

                var checkCode = getAllStation.FirstOrDefault(x => x.Code == request.Code && x.Id != Guid.Parse(stationId));
                if (checkCode != null)
                    throw new ErrorResponse(404, (int)StationErrorEnums.CODE_EXIST,
                       StationErrorEnums.CODE_EXIST.GetDisplayName());

                var checkFloor = await _unitOfWork.Repository<Floor>().GetAll().FirstOrDefaultAsync(x => x.Id == request.FloorId);
                if (checkFloor == null)
                    throw new ErrorResponse(404, (int)FloorErrorEnums.NOT_FOUND,
                       FloorErrorEnums.NOT_FOUND.GetDisplayName());

                //lấy ds area trong json
                string jsonFilePath = "Configuration\\listArea.json";
                string jsonString = File.ReadAllText(jsonFilePath);
                var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(jsonString);
                var allAreaCode = jsonObject["AreaCodes"];

                var areaCode = allAreaCode.FirstOrDefault(x => x == request.AreaCode);
                if (areaCode == null)
                    throw new ErrorResponse(404, (int)AreaErrorEnums.NOT_FOUND_CODE,
                  AreaErrorEnums.NOT_FOUND_CODE.GetDisplayName());

                var updateStation = _mapper.Map<UpdateStationRequest, Station>(request, station);

                updateStation.AreaCode = areaCode;
                updateStation.UpdateAt = DateTime.Now;

                await _unitOfWork.Repository<Station>().UpdateDetached(updateStation);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<StationResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<StationResponse>(updateStation)
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}

