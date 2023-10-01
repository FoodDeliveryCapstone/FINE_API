﻿using AutoMapper;
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

namespace FINE.Service.Service
{
    public interface IBoxService
    {
        Task<BaseResponseViewModel<OrderBoxResponse>> AddOrderToBox(string stationId, string key, AddOrderToBoxRequest request);
        Task<BaseResponseViewModel<OrderByStoreResponse>> SystemAddOrderToBox(SystemAddOrderToBoxRequest request);
        Task<BaseResponsePagingViewModel<BoxResponse>> GetBoxByStation(string stationId, BoxResponse filter, PagingRequest paging);

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

        public async Task<BaseResponseViewModel<OrderBoxResponse>> AddOrderToBox(string stationId, string key, AddOrderToBoxRequest request)
        {
            try
            {
                //var key = Utils.GenerateRandomCode(10);
                var activeBox = await _unitOfWork.Repository<Box>().GetAll()
                    .Where(x => x.IsActive == true && x.StationId == Guid.Parse(stationId))
                    .ToListAsync();
                if (activeBox == null)
                    throw new ErrorResponse(404, (int)BoxErrorEnums.BOX_NOT_AVAILABLE,
                       BoxErrorEnums.BOX_NOT_AVAILABLE.GetDisplayName());
                var checkOrderBox = await _unitOfWork.Repository<OrderBox>().GetAll().FirstOrDefaultAsync(x => x.OrderId == request.OrderId);
                if (checkOrderBox != null)
                    throw new ErrorResponse(404, (int)OrderBoxErrorEnums.ORDER_BOX_EXISTED,
                       OrderBoxErrorEnums.ORDER_BOX_EXISTED.GetDisplayName());

                var order = await _unitOfWork.Repository<Order>().GetAll()
                                .FirstOrDefaultAsync(x => x.Id == request.OrderId);                   
                if (order == null)
                    throw new ErrorResponse(404, (int)OrderErrorEnums.NOT_FOUND,
                        OrderErrorEnums.NOT_FOUND.GetDisplayName());

                var orderBox = new OrderBox()
                {
                    Id = Guid.NewGuid(),
                    OrderId = request.OrderId,
                    BoxId = request.BoxId,
                    Key = key,
                    Status = (int)OrderBoxStatusEnum.NotPicked,
                    CreateAt = DateTime.Now,
                };
                await _unitOfWork.Repository<OrderBox>().InsertAsync(orderBox);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<OrderBoxResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
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

        public async Task<BaseResponseViewModel<OrderByStoreResponse>> SystemAddOrderToBox(SystemAddOrderToBoxRequest request)
        {
            try
            {
                var key = Utils.GenerateRandomCode(10);
                var box = await _unitOfWork.Repository<Box>().GetAll().ToListAsync();
                var getAllOrder = await _unitOfWork.Repository<Data.Entity.Order>().GetAll().ToListAsync();
                var preBoxId = Guid.Empty;
                int nextBoxIndex = 0;
                foreach (var item in request.OrderId)
                {
                    List<OrderByStoreResponse> checkOrderStatus = await ServiceHelpers.GetSetDataRedisOrder(RedisSetUpType.GET, item.ToString());
                    if (!checkOrderStatus.Any(x => x.OrderDetailStoreStatus != OrderStatusEnum.Delivering))
                    {
                        HashSet<Guid> orderIdList = new HashSet<Guid>();
                        var order = getAllOrder.FirstOrDefault(x => x.Id == item);
                        var boxByStation = box
                            .Where(x => x.StationId == order.StationId)
                            .OrderBy(x => x.CreateAt)
                            .ToList();
                        // lay box tiep theo trong boxByStation
                        if (boxByStation.Any())
                        {
                            if (nextBoxIndex < boxByStation.Count)
                            {
                                var nextBox = boxByStation[nextBoxIndex];

                                var addOrderToBoxRequest = new AddOrderToBoxRequest()
                                {
                                    BoxId = nextBox.Id,
                                    OrderId = order.Id
                                };
                                var addOrderToBox = await AddOrderToBox(order.StationId.ToString(), key, addOrderToBoxRequest);

                                preBoxId = nextBox.Id;
                                nextBoxIndex++;
                            }
                        }
                    }
                    else
                    {
                        throw new ErrorResponse(400, (int)OrderErrorEnums.CANNOT_UPDATE_ORDER,
                        OrderErrorEnums.CANNOT_UPDATE_ORDER.GetDisplayName());
                    }
                    //}
                }

                return new BaseResponseViewModel<OrderByStoreResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    //Data =
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}
