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
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using static FINE.Service.Helpers.Enum;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IStationService
    {
        Task<BaseResponsePagingViewModel<StationResponse>> GetStationByDestination(string destinationId, PagingRequest paging);
        Task<BaseResponseViewModel<dynamic>> GetStationByDestinationForOrder(string destinationId, string orderCode, int numberBox);
        Task<BaseResponseViewModel<StationResponse>> GetStationById(string stationId);
        Task<BaseResponseViewModel<StationResponse>> CreateStation(CreateStationRequest request);
        Task<BaseResponseViewModel<StationResponse>> UpdateStation(string stationId, UpdateStationRequest request);
        Task<BaseResponseViewModel<int>> LockBox(string stationId, string orderCode, int numberBox);
        Task<BaseResponseViewModel<dynamic>> UpdateLockBox(LockBoxUpdateTypeEnum type, string orderCode, string? stationId = null);
        Task<BaseResponsePagingViewModel<StationResponse>> GetStationByDestinationForAdmin(string destinationId, PagingRequest paging);
    }

    public class StationService : IStationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        public StationService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
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

        public async Task<BaseResponseViewModel<dynamic>> GetStationByDestinationForOrder(string destinationId, string orderCode ,int numberBox)
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
                        NumberBoxLockPending = 0,
                        ListBoxId = new List<Guid>(),
                        ListOrderBox = new List<KeyValuePair<string, List<Guid>>>()
                    }).ToList();
                }
                foreach (var stationLock in listStationLockBox)
                {
                    if(stationLock.ListBoxId is null)
                    {
                        stationLock.ListBoxId = new List<Guid>();
                        stationLock.ListOrderBox = new List<KeyValuePair<string, List<Guid>>>();
                    }
                    //get các station còn available kể cả box lock
                    var stationDb = await listStation.FirstOrDefaultAsync(x => x.Id == stationLock.StationId
                                                && x.Boxes.Where(x => x.IsActive == true
                                                && x.OrderBoxes.Any(y => y.Status != (int)OrderBoxStatusEnum.Picked) == false).Count()
                                                - stationLock.NumberBoxLockPending >= numberBox);
                    if (stationDb is not null)
                    {
                        var stationFit = _mapper.Map<StationResponse>(stationDb);
                        var pickedBox = stationDb.Boxes.Where(x => x.IsActive == true && x.OrderBoxes.Any(y => y.Status != (int)OrderBoxStatusEnum.Picked) == false)
                                                    .Select(x => x.Id).Except(stationLock.ListBoxId)
                                                    .Take(numberBox)
                                                    .ToList();

                        stationLock.NumberBoxLockPending += numberBox;
                        stationLock.ListBoxId.AddRange(pickedBox);
                        stationLock.ListOrderBox.Add(new KeyValuePair<string, List<Guid>>(orderCode, pickedBox));

                        result.Add(stationFit);
                    }
                }
                int countDount = Int32.Parse(_configuration["CountDownPayment"]);
                await ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, key, listStationLockBox);

                return new BaseResponseViewModel<dynamic>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = new
                    {
                        CountDown = countDount,
                        ListStation = result
                    }
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<int>> LockBox(string stationId, string orderCode, int numberBox)
        {
            try
            {
                var station = await _unitOfWork.Repository<Station>().GetAll().FirstOrDefaultAsync(x => x.Id == Guid.Parse(stationId));
                var key = RedisDbEnum.Box.GetDisplayName() + ":Station";

                List<LockBoxinStationModel> listStationLockBox = new List<LockBoxinStationModel>();
                var redisValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, key, null);
                if (redisValue.HasValue == true)
                {
                    listStationLockBox = JsonConvert.DeserializeObject<List<LockBoxinStationModel>>(redisValue);
                }

                var keyOrder = RedisDbEnum.Box.GetDisplayName() + ":Order:" + orderCode;
                List<Guid> listBoxOrder = new List<Guid>();

                var orderBox = listStationLockBox.FirstOrDefault(x => x.StationId == Guid.Parse(stationId)).ListOrderBox.FirstOrDefault(x => x.Key == orderCode).Value;
                listBoxOrder.AddRange(orderBox);

                await ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, keyOrder, listBoxOrder);
                return new BaseResponseViewModel<int>()
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

        public async Task<BaseResponseViewModel<dynamic>> UpdateLockBox(LockBoxUpdateTypeEnum type, string orderCode, string? stationId = null)
        {
            try
            {
                var keyOrder = RedisDbEnum.Box.GetDisplayName() + ":Order:" + orderCode;
                List<Guid> listLockOrder = new List<Guid>();
                var redisValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyOrder, null);
                if (redisValue.HasValue == true)
                {
                    listLockOrder = JsonConvert.DeserializeObject<List<Guid>>(redisValue);
                }
                var numberBox = listLockOrder.Count();

                var key = RedisDbEnum.Box.GetDisplayName() + ":Station";

                List<LockBoxinStationModel> listStationLockBox = new List<LockBoxinStationModel>();
                var redisStationValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, key, null);
                if (redisStationValue.HasValue == true)
                {
                    listStationLockBox = JsonConvert.DeserializeObject<List<LockBoxinStationModel>>(redisStationValue);
                }

                switch (type)
                {
                    case LockBoxUpdateTypeEnum.Delete:

                        foreach (var station in listStationLockBox)
                        {
                            station.NumberBoxLockPending -= numberBox;
                            station.ListBoxId = station.ListBoxId.Except(listLockOrder).ToList();
                            station.ListOrderBox.RemoveAll(x => x.Key == orderCode);

                            listStationLockBox = listStationLockBox.Select(x => new LockBoxinStationModel
                            {
                                StationName = x.StationName,
                                StationId = x.StationId,
                                NumberBoxLockPending = x.NumberBoxLockPending - numberBox,
                            }).ToList();
                        }

                        await ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, key, listStationLockBox);
                        await ServiceHelpers.GetSetDataRedis(RedisSetUpType.DELETE, keyOrder, null);
                        break;

                    case LockBoxUpdateTypeEnum.Change:

                        var orderBox = listStationLockBox.FirstOrDefault(x => x.StationId == Guid.Parse(stationId)).ListOrderBox.FirstOrDefault(x => x.Key == orderCode).Value;

                        listLockOrder.Clear();
                        listLockOrder.AddRange(orderBox);
                        await ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, keyOrder, listLockOrder);
                        break;
                }
                return new BaseResponseViewModel<dynamic>()
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

        public async Task<BaseResponsePagingViewModel<StationResponse>> GetStationByDestinationForAdmin(string destinationId, PagingRequest paging)
        {
            try
            {
                var checkDestination = _unitOfWork.Repository<Destination>().GetAll().Any(x => x.Id == Guid.Parse(destinationId));
                if (checkDestination == false)
                    throw new ErrorResponse(404, (int)StationErrorEnums.NOT_FOUND,
                       StationErrorEnums.NOT_FOUND.GetDisplayName());

                var stations = _unitOfWork.Repository<Station>().GetAll()
                                .Where(x => x.Floor.DestionationId == Guid.Parse(destinationId))
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
    }
}

