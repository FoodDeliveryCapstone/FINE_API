using AutoMapper;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Box;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Helpers;
using FINE.Service.Utilities;
using System.Drawing;
using ZXing.QrCode;
using ZXing;
using static FINE.Service.Helpers.Enum;
using static FINE.Service.Helpers.ErrorEnum;
using ZXing.Windows.Compatibility;
using AutoMapper.QueryableExtensions;
using FINE.Service.Attributes;
using FINE.Service.DTO.Request.Order;
using Microsoft.EntityFrameworkCore;
using FINE.Service.DTO.Request.Station;
using System.Net.NetworkInformation;
using StackExchange.Redis;

namespace FINE.Service.Service
{
    public interface IBoxService
    {
        Task<BaseResponsePagingViewModel<BoxResponse>> GetBoxByStation(string stationId, BoxResponse filter, PagingRequest paging);
        Task<BaseResponseViewModel<BoxResponse>> CreateBox(CreateBoxRequest request);
        Task<BaseResponseViewModel<BoxResponse>> UpdateBox(string boxId, UpdateBoxRequest request);
        Task<BaseResponsePagingViewModel<AvailableBoxInStationResponse>> GetAvailableBoxInStation(string timeslotId);

    }

    public class BoxService : IBoxService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public BoxService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponsePagingViewModel<BoxResponse>> GetBoxByStation(string stationId, BoxResponse filter, PagingRequest paging)
        {
            try 
            { 
                var station = _unitOfWork.Repository<Station>().GetAll()
                    .FirstOrDefault(x => x.Id == Guid.Parse(stationId));
                if (station == null)
                    throw new ErrorResponse(404, (int)StationErrorEnums.NOT_FOUND,
                        StationErrorEnums.NOT_FOUND.GetDisplayName());

                var box = _unitOfWork.Repository<Box>().GetAll()
                    .Where(x => x.StationId == Guid.Parse(stationId))
                    .OrderBy(x => x.CreateAt)
                    .ProjectTo<BoxResponse>(_mapper.ConfigurationProvider)
                    .DynamicFilter(filter)
                    .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);

                return new BaseResponsePagingViewModel<BoxResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Page = paging.Page,
                        Size = paging.PageSize,
                        Total = box.Item1
                    },
                    Data = box.Item2.ToList()
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<BoxResponse>> CreateBox(CreateBoxRequest request)
        {
            try
            {
                var checkCode = await _unitOfWork.Repository<Box>().GetAll().FirstOrDefaultAsync(x => x.Code == request.Code);
                if (checkCode != null)
                    throw new ErrorResponse(404, (int)BoxErrorEnums.CODE_EXIST,
                       BoxErrorEnums.CODE_EXIST.GetDisplayName());

                var box = _mapper.Map<CreateBoxRequest, Box>(request);

                box.Id = Guid.NewGuid();
                box.IsActive = true;
                box.CreateAt = DateTime.Now;

                await _unitOfWork.Repository<Box>().InsertAsync(box);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<BoxResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<BoxResponse>(box)
                };
            }
            catch(ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<BoxResponse>> UpdateBox(string boxId, UpdateBoxRequest request)
        {
            try
            {
                var getAllBox = _unitOfWork.Repository<Box>().GetAll();
                var box = getAllBox.FirstOrDefault(x => x.Id == Guid.Parse(boxId));
                if (box == null)
                    throw new ErrorResponse(404, (int)BoxErrorEnums.NOT_FOUND,
                        BoxErrorEnums.NOT_FOUND.GetDisplayName());

                var checkCode = getAllBox.FirstOrDefault(x => x.Code == request.Code && x.Id != Guid.Parse(boxId));
                if (checkCode != null)
                    throw new ErrorResponse(404, (int)BoxErrorEnums.CODE_EXIST,
                       BoxErrorEnums.CODE_EXIST.GetDisplayName());

                var updateBox = _mapper.Map<UpdateBoxRequest, Box>(request, box);

                updateBox.UpdateAt = DateTime.Now;

                await _unitOfWork.Repository<Box>().UpdateDetached(updateBox);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<BoxResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<BoxResponse>(updateBox)
                };
            }
            catch(ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponsePagingViewModel<AvailableBoxInStationResponse>> GetAvailableBoxInStation(string timeslotId)
        {
            try
            {
                List<AvailableBoxResponse> availableBoxes = new List<AvailableBoxResponse>();
                var getAllBoxInStation = await _unitOfWork.Repository<Box>().GetAll()
                                            .Where(x => x.IsActive == true)
                                            .OrderBy(x => x.CreateAt)
                                            .ToListAsync();

                var getOrderBox = await _unitOfWork.Repository<OrderBox>().GetAll()
                                  .Include(x => x.Order)
                                  .Where(x => x.Order.TimeSlotId == Guid.Parse(timeslotId))
                                  .ToListAsync();

                foreach (var box in getAllBoxInStation)
                {
                    var getBoxStatus = getOrderBox.Where(x => x.BoxId == box.Id)
                                            .OrderByDescending(x => x.CreateAt)
                                            .FirstOrDefault();
                    if (getBoxStatus == null)
                    {
                        var availablebox = new AvailableBoxResponse
                        {
                            Id = box.Id,
                            Code = box.Code,
                            Status = (int)OrderBoxStatusEnum.Picked,
                            IsHeat = box.IsHeat,
                            StationId = box.StationId,
                        };
                        availableBoxes.Add(availablebox);
                    }
                    else
                    {
                        var availablebox = new AvailableBoxResponse
                        {
                            Id = box.Id,
                            Code = box.Code,
                            Status = getBoxStatus.Status,
                            IsHeat = box.IsHeat,
                            StationId = box.StationId,
                        };
                        availableBoxes.Add(availablebox);
                    }
                }

                var station = await _unitOfWork.Repository<Station>().GetAll().ToListAsync();
                var availableBoxInStation = availableBoxes
                                            .GroupBy(stationId => stationId.StationId)
                                            .Select(group => new AvailableBoxInStationResponse
                                            {
                                                StationId = group.Key,
                                                StationName = station.FirstOrDefault(x => x.Id == group.Key).Name,
                                                ListBox = group.Select(detail => new AvailableBoxResponse
                                                {
                                                    Id= detail.Id,
                                                    Code = detail.Code,
                                                    Status = detail.Status,
                                                    IsHeat = detail.IsHeat,
                                                    StationId = detail.StationId,
                                                }).ToList(),
                                            }).ToList();


                return new BaseResponsePagingViewModel<AvailableBoxInStationResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Total = availablebox.Count
                    },
                    Data = availableBoxInStation
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}
