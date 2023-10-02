using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Attributes;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Box;
using FINE.Service.DTO.Request.Order;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Helpers;
using FINE.Service.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IOrderDetailService
    {
        Task<BaseResponsePagingViewModel<OrderDetailResponse>> GetOrdersDetailByStore(string storeId, int orderStatus, PagingRequest paging);
        Task<BaseResponsePagingViewModel<OrderByStoreResponse>> GetSplitOrderDetail( string stationId, int status, string timeslot = null, string storeId = null);
        Task<BaseResponseViewModel<OrderByStoreResponse>> UpdateOrderByStoreStatus(UpdateOrderDetailStatusRequest request);
        Task<BaseResponsePagingViewModel<OrderByStoreResponse>> GetStaffOrderDetailByOrderId(string orderId);
        Task<BaseResponsePagingViewModel<SplitOrderResponse>> GetSplitOrder(string storeId, string timeslotId, int status,string stationId = null);
        Task<BaseResponsePagingViewModel<ShipperSplitOrderResponse>> GetShipperSplitOrder(string timeslotId, string stationId, string? storeId, int? status);
        Task<BaseResponsePagingViewModel<ShipperOrderBoxResponse>> GetShipperOrderBox(string stationId, string timeslotId);

    }

    public class OrderDetailService : IOrderDetailService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IStaffService _staffService;
        private readonly IBoxService _boxService;
        public OrderDetailService(IUnitOfWork unitOfWork, IMapper mapper, IStaffService staffService, IBoxService boxService)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _staffService = staffService;
            _boxService = boxService;
        }

        public async Task<BaseResponsePagingViewModel<OrderDetailResponse>> GetOrdersDetailByStore(string storeId, int orderStatus, PagingRequest paging)
        {
            try
            {
                var order = _unitOfWork.Repository<OrderDetail>().GetAll()
                                        .Where(x => x.StoreId == Guid.Parse(storeId) && x.Order.OrderStatus == orderStatus)
                                        .OrderBy(x => x.OrderId)
                                        .ThenBy(x => x.Order.CheckInDate)
                                        .ProjectTo<OrderDetailResponse>(_mapper.ConfigurationProvider)
                                        .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging,
                                        Constants.DefaultPaging);

                return new BaseResponsePagingViewModel<OrderDetailResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Page = paging.Page,
                        Size = paging.PageSize,
                        Total = order.Item1
                    },
                    Data = order.Item2.ToList()

                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponsePagingViewModel<OrderByStoreResponse>> GetSplitOrderDetail(string stationId, int status, string timeslotId = null, string storeId = null)
        {
            try
            {
                // Get from Redis
                List<OrderByStoreResponse> orderResponse = await ServiceHelpers.GetSetDataRedisOrder(RedisSetUpType.GET);
                if (timeslotId == null && storeId == null)
                {
                    orderResponse = orderResponse.Where(x => x.StationId == Guid.Parse(stationId)
                                                                 && (int)x.OrderDetailStoreStatus == status)
                                                                 .OrderByDescending(x => x.CheckInDate)
                                                                 .ToList();
                }
               
                else if (storeId == null)
                {
                    orderResponse = orderResponse.Where(x => Guid.Parse(x.TimeSlot.Id) == Guid.Parse(timeslotId)
                                                                 && x.StationId == Guid.Parse(stationId)
                                                                 && (int)x.OrderDetailStoreStatus == status)
                                                                 .OrderByDescending(x => x.CheckInDate)
                                                                 .ToList();
                }
                else if (timeslotId == null)
                {
                    orderResponse = orderResponse.Where(x => x.StoreId == Guid.Parse(storeId)
                                             && x.StationId == Guid.Parse(stationId)
                                             && (int)x.OrderDetailStoreStatus == status)
                                             .OrderByDescending(x => x.CheckInDate)
                                             .ToList();
                }
                else 
                {
                    orderResponse = orderResponse.Where(x => x.StoreId == Guid.Parse(storeId) 
                                             && x.StationId == Guid.Parse(stationId)
                                             && (int)x.OrderDetailStoreStatus == status
                                             && Guid.Parse(x.TimeSlot.Id) == Guid.Parse(timeslotId))
                                             .OrderByDescending(x => x.CheckInDate)
                                             .ToList();
                }

                return new BaseResponsePagingViewModel<OrderByStoreResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Total = orderResponse.Count()
                    },
                    Data = orderResponse
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponsePagingViewModel<OrderByStoreResponse>> GetStaffOrderDetailByOrderId(string orderId)
        {
            try
            {
                List<OrderByStoreResponse> orderResponse = await ServiceHelpers.GetSetDataRedisOrder(RedisSetUpType.GET, orderId);
                var order = orderResponse.Where(x => x.OrderId == Guid.Parse(orderId));

                return new BaseResponsePagingViewModel<OrderByStoreResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Total = orderResponse.Count()
                    },
                    Data = order.ToList()
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }

        }

        public async Task<BaseResponseViewModel<OrderByStoreResponse>> UpdateOrderByStoreStatus(UpdateOrderDetailStatusRequest request)
        {
            try
            {
                var key = Utils.GenerateRandomCode(10);
                var box = await _unitOfWork.Repository<Box>().GetAll().ToListAsync();
                var getAllOrder = await _unitOfWork.Repository<Data.Entity.Order>().GetAll().ToListAsync();
                var preBoxId = Guid.Empty;
                int nextBoxIndex = 0;

                foreach (var item in request.ListStoreAndOrder)
                {
                    List<OrderByStoreResponse> orderResponse = await ServiceHelpers.GetSetDataRedisOrder(RedisSetUpType.GET, item.OrderId.ToString());
                    orderResponse = orderResponse.OrderByDescending(x => x.CheckInDate)
                                                     .ToList();
                    var order = orderResponse.FirstOrDefault(x => x.OrderId == item.OrderId && x.StoreId == item.StoreId);
                    //cannot update to previous status 
                    if((int)order.OrderDetailStoreStatus > (int)request.OrderDetailStoreStatus)
                        throw new ErrorResponse(404, (int)OrderErrorEnums.CANNOT_UPDATE_ORDER,
                           OrderErrorEnums.CANNOT_UPDATE_ORDER.GetDisplayName());

                    order.OrderDetailStoreStatus = request.OrderDetailStoreStatus;
                    
                    ServiceHelpers.GetSetDataRedisOrder(RedisSetUpType.SET, order.OrderId.ToString(), orderResponse);
             
                    #region check Order status and update
                    List<OrderByStoreResponse> checkOrderStatus = await ServiceHelpers.GetSetDataRedisOrder(RedisSetUpType.GET, item.OrderId.ToString());
                    //check if every order detail in a order have change status then change order status
                    if (!checkOrderStatus.Any(x => x.OrderDetailStoreStatus != OrderStatusEnum.FinishPrepare))
                    {
                        var updateOrderStatusRequest = new UpdateOrderStatusRequest()
                        {
                            OrderStatus = OrderStatusEnum.FinishPrepare
                        };
                        var updateOrder = await _staffService.UpdateOrderStatus(order.OrderId.ToString(), updateOrderStatusRequest);

                    }
                    else if (!checkOrderStatus.Any(x => x.OrderDetailStoreStatus != OrderStatusEnum.Delivering))
                    {
                        var updateOrderStatusRequest = new UpdateOrderStatusRequest()
                        {
                            OrderStatus = OrderStatusEnum.Delivering
                        };
                        var updateOrder = await _staffService.UpdateOrderStatus(order.OrderId.ToString(), updateOrderStatusRequest);
                       
                    }
                    else if (!checkOrderStatus.Any(x => x.OrderDetailStoreStatus != OrderStatusEnum.BoxStored))
                    {
                        var updateOrderStatusRequest = new UpdateOrderStatusRequest()
                        {
                            OrderStatus = OrderStatusEnum.BoxStored
                        };
                        var updateOrder = await _staffService.UpdateOrderStatus(order.OrderId.ToString(), updateOrderStatusRequest);
                        ServiceHelpers.GetSetDataRedisOrder(RedisSetUpType.DELETE, order.OrderId.ToString());
                    }
                    #endregion               
                }

                //if (request.OrderDetailStoreStatus == OrderStatusEnum.Delivering)
                //{
                //    HashSet<Guid> orderIdList = new HashSet<Guid>();
                //    foreach (var newEntityOrder in request.ListStoreAndOrder)
                //    {
                //        if (!orderIdList.Equals(newEntityOrder.OrderId))
                //        {
                //            orderIdList.Add(newEntityOrder.OrderId);
                //        }
                //    }
                //    foreach (var orderId in orderIdList)
                //    {
                //        var newOrder = getAllOrder.FirstOrDefault(x => x.Id == orderId);
                //        var boxByStation = box
                //       .Where(x => x.StationId == newOrder.StationId)
                //       .OrderBy(x => x.CreateAt)
                //       .ToList();
                //        // lay box tiep theo trong boxByStation
                //        if (boxByStation.Any())
                //        {
                //            if (nextBoxIndex < boxByStation.Count)
                //            {
                //                var nextBox = boxByStation[nextBoxIndex];

                //                var addOrderToBoxRequest = new AddOrderToBoxRequest()
                //                {
                //                    BoxId = nextBox.Id,
                //                    OrderId = newOrder.Id
                //                };
                //                var addOrderToBox = await _boxService.AddOrderToBox(newOrder.StationId.ToString(), key, addOrderToBoxRequest);

                //                preBoxId = nextBox.Id;
                //                nextBoxIndex++;
                //            }
                //        }
                //    }
                //}
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
            catch(ErrorResponse ex)
            {
                throw ex;
            }
        }
        public async Task<BaseResponsePagingViewModel<SplitOrderResponse>> GetSplitOrder(string storeId, string timeslotId, int status, string stationId = null)
        {
            try
            {
                List<OrderByStoreResponse> orderResponse = await ServiceHelpers.GetSetDataRedisOrder(RedisSetUpType.GET);                
                var productInMenuList = new List<SplitOrderResponse>();
                if (stationId == null)
                {
                    orderResponse = orderResponse.Where(x => x.StoreId == Guid.Parse(storeId)
                                            && Guid.Parse(x.TimeSlot.Id) == Guid.Parse(timeslotId)
                                            && (int)x.OrderDetailStoreStatus == status)
                                            .OrderByDescending(x => x.CheckInDate)
                                            .ToList();
                    foreach (var order in orderResponse)
                    {
                        productInMenuList.AddRange(order.OrderDetails.Select(x => new SplitOrderResponse
                        {
                            ProductName = x.ProductName,
                            Quantity = x.Quantity,
                            TimeSlotId = Guid.Parse(order.TimeSlot.Id),
                        })
                        .ToList()); 
                    }
                    productInMenuList = productInMenuList
                        .GroupBy(x => new {
                            x.ProductName,
                            x.TimeSlotId
                        })
                        .Select(x => new SplitOrderResponse
                        {
                            ProductName = x.Key.ProductName,
                            Quantity = x.Sum(x => x.Quantity),
                            TimeSlotId = x.Key.TimeSlotId
                        })
                        .ToList();
                }
                else
                {
                    orderResponse = orderResponse.Where(x => x.StoreId == Guid.Parse(storeId) 
                                             && x.StationId == Guid.Parse(stationId)
                                             && Guid.Parse(x.TimeSlot.Id) == Guid.Parse(timeslotId)
                                             && (int)x.OrderDetailStoreStatus == status)
                                             .OrderByDescending(x => x.CheckInDate)
                                             .ToList();
                    foreach (var order in orderResponse)
                    {
                        productInMenuList.AddRange(order.OrderDetails.Select(x => new SplitOrderResponse
                        {
                            ProductName = x.ProductName,
                            Quantity = x.Quantity,
                            TimeSlotId = Guid.Parse(order.TimeSlot.Id),
                        })
                        .ToList());
                    }
                    productInMenuList = productInMenuList
                        .GroupBy(x => new {
                            x.ProductName,
                            x.TimeSlotId
                        })
                        .Select(x => new SplitOrderResponse
                        {
                            ProductName = x.Key.ProductName,
                            Quantity = x.Sum(x => x.Quantity),
                            TimeSlotId = x.Key.TimeSlotId
                        })
                        .ToList();
                }

                return new BaseResponsePagingViewModel<SplitOrderResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Total = productInMenuList.Count()
                    },
                    Data = productInMenuList
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponsePagingViewModel<ShipperSplitOrderResponse>> GetShipperSplitOrder(string timeslotId, string stationId, string? storeId, int? status)
        {
            try
            {
                List<OrderByStoreResponse> orderResponse = await ServiceHelpers.GetSetDataRedisOrder(RedisSetUpType.GET);
                var productInMenuList = new List<ShipperSplitOrderResponse>();

                if (status == null && storeId == null)
                {
                    orderResponse = orderResponse.Where(x => Guid.Parse(x.TimeSlot.Id) == Guid.Parse(timeslotId)
                                                 && x.StationId == Guid.Parse(stationId))
                                                .OrderByDescending(x => x.CheckInDate)
                                                .ToList();
                    if (orderResponse == null)
                        throw new ErrorResponse(404, (int)OrderErrorEnums.NOT_FOUND,
                               OrderErrorEnums.NOT_FOUND.GetDisplayName());
                    foreach (var order in orderResponse)
                    {
                        productInMenuList.AddRange(order.OrderDetails.Select(x => new ShipperSplitOrderResponse
                        {
                            TimeSlotId = Guid.Parse(order.TimeSlot.Id),
                            StationId = order.StationId,
                            StoreId = x.StoreId,
                            ProductName = x.ProductName,
                            Quantity = x.Quantity,
                        })
                        .ToList());
                    }
                    productInMenuList = productInMenuList
                        .GroupBy(x => new
                        {
                            x.ProductName,
                            x.TimeSlotId,
                            x.StationId,
                            x.StoreId,
                        })
                        .Select(x => new ShipperSplitOrderResponse
                        {
                            ProductName = x.Key.ProductName,
                            Quantity = x.Sum(x => x.Quantity),
                            StoreId = x.Key.StoreId,
                            TimeSlotId = x.Key.TimeSlotId,
                            StationId = x.Key.StationId
                        })
                        .ToList();
                }
                else if (storeId == null)
                {
                    orderResponse = orderResponse.Where(x => Guid.Parse(x.TimeSlot.Id) == Guid.Parse(timeslotId)
                                                 && x.StationId == Guid.Parse(stationId)
                                                 && (int)x.OrderDetailStoreStatus == status)
                                                .OrderByDescending(x => x.CheckInDate)
                                                .ToList();
                    if (orderResponse == null)
                        throw new ErrorResponse(404, (int)OrderErrorEnums.NOT_FOUND,
                               OrderErrorEnums.NOT_FOUND.GetDisplayName());
                    foreach (var order in orderResponse)
                    {
                        productInMenuList.AddRange(order.OrderDetails.Select(x => new ShipperSplitOrderResponse
                        {
                            TimeSlotId = Guid.Parse(order.TimeSlot.Id),
                            StationId = order.StationId,
                            StoreId = x.StoreId,
                            ProductName = x.ProductName,
                            Quantity = x.Quantity,
                        })
                        .ToList());
                    }
                    productInMenuList = productInMenuList
                        .GroupBy(x => new
                        {
                            x.ProductName,
                            x.TimeSlotId,
                            x.StationId,
                            x.StoreId,
                        })
                        .Select(x => new ShipperSplitOrderResponse
                        {
                            ProductName = x.Key.ProductName,
                            Quantity = x.Sum(x => x.Quantity),
                            StoreId = x.Key.StoreId,
                            TimeSlotId = x.Key.TimeSlotId,
                            StationId = x.Key.StationId
                        })
                        .ToList();
                }
                else if(status == null)
                {
                    orderResponse = orderResponse.Where(x => Guid.Parse(x.TimeSlot.Id) == Guid.Parse(timeslotId)
                                                 && x.StationId == Guid.Parse(stationId)
                                                 && x.StoreId == Guid.Parse(storeId))
                                                .OrderByDescending(x => x.CheckInDate)
                                                .ToList();
                    if (orderResponse == null)
                        throw new ErrorResponse(404, (int)OrderErrorEnums.NOT_FOUND,
                               OrderErrorEnums.NOT_FOUND.GetDisplayName());
                    foreach (var order in orderResponse)
                    {
                        productInMenuList.AddRange(order.OrderDetails.Select(x => new ShipperSplitOrderResponse
                        {
                            TimeSlotId = Guid.Parse(order.TimeSlot.Id),
                            StationId = order.StationId,
                            StoreId = x.StoreId,
                            ProductName = x.ProductName,
                            Quantity = x.Quantity,
                        })
                        .ToList());
                    }
                    productInMenuList = productInMenuList
                        .GroupBy(x => new
                        {
                            x.ProductName,
                            x.TimeSlotId,
                            x.StationId,
                            x.StoreId,
                        })
                        .Select(x => new ShipperSplitOrderResponse
                        {
                            ProductName = x.Key.ProductName,
                            Quantity = x.Sum(x => x.Quantity),
                            StoreId = x.Key.StoreId,
                            TimeSlotId = x.Key.TimeSlotId,
                            StationId = x.Key.StationId
                        })
                        .ToList();
                }
                
                else
                {
                    orderResponse = orderResponse.Where(x => Guid.Parse(x.TimeSlot.Id) == Guid.Parse(timeslotId)
                                                 && x.StationId == Guid.Parse(stationId)
                                                 && x.StoreId == Guid.Parse(storeId)
                                                 && (int)x.OrderDetailStoreStatus == status)
                                                .OrderByDescending(x => x.CheckInDate)
                                                .ToList();
                    if (orderResponse == null)
                        throw new ErrorResponse(404, (int)OrderErrorEnums.NOT_FOUND,
                               OrderErrorEnums.NOT_FOUND.GetDisplayName());
                    foreach (var order in orderResponse)
                    {
                        productInMenuList.AddRange(order.OrderDetails.Select(x => new ShipperSplitOrderResponse
                        {
                            TimeSlotId = Guid.Parse(order.TimeSlot.Id),
                            StationId = order.StationId,
                            StoreId = x.StoreId,
                            ProductName = x.ProductName,
                            Quantity = x.Quantity,
                        })
                        .ToList());
                    }
                    productInMenuList = productInMenuList
                        .GroupBy(x => new
                        {
                            x.ProductName,
                            x.TimeSlotId,
                            x.StationId,
                            x.StoreId,
                        })
                        .Select(x => new ShipperSplitOrderResponse
                        {
                            ProductName = x.Key.ProductName,
                            Quantity = x.Sum(x => x.Quantity),
                            StoreId = x.Key.StoreId,
                            TimeSlotId = x.Key.TimeSlotId,
                            StationId = x.Key.StationId
                        })
                        .ToList();
                }

                return new BaseResponsePagingViewModel<ShipperSplitOrderResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Total = productInMenuList.Count()
                    },
                    Data = productInMenuList
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponsePagingViewModel<ShipperOrderBoxResponse>> GetShipperOrderBox(string stationId, string timeslotId)
        {
            try
            { 

                List<OrderByStoreResponse> orderResponse = await ServiceHelpers.GetSetDataRedisOrder(RedisSetUpType.GET);
                orderResponse = orderResponse.Where(x => x.StationId == Guid.Parse(stationId)
                                              && Guid.Parse(x.TimeSlot.Id) == Guid.Parse(timeslotId)
                                              && (int)x.OrderDetailStoreStatus == (int)OrderStatusEnum.Delivering)
                                             .ToList();
                var orderBox = await _unitOfWork.Repository<OrderBox>().GetAll().ToListAsync();
                var orderBoxList = new List<ShipperOrderBoxResponse>();
                Guid prevBoxId = Guid.Empty;
                var orderDetail = new List<OrderDetailResponse>();

                foreach(var order in orderResponse)
                {
                    var box = orderBox.FirstOrDefault(x => x.OrderId == order.OrderId);   
                    if (box == null)
                        throw new ErrorResponse(404, (int)BoxErrorEnums.NOT_FOUND_ORDERBOX,
                           BoxErrorEnums.NOT_FOUND_ORDERBOX.GetDisplayName());
                    //check box id truoc
                    if (prevBoxId != box.BoxId)
                    {
                        //khac thi tao lai list roi add theo box id moi
                        orderDetail = new List<OrderDetailResponse>();
                        orderDetail.AddRange(order.OrderDetails);
                        orderBoxList.Add(new ShipperOrderBoxResponse
                        {
                            BoxId = box.BoxId,
                            OrderDetails = orderDetail
                        });
                    }
                    else
                    {
                        //khong khac thi add roi them vao lai order detail
                        orderDetail.AddRange(order.OrderDetails);
                        orderBoxList.FirstOrDefault(x => x.BoxId == prevBoxId).OrderDetails = orderDetail;
                    }

                    prevBoxId = box.BoxId;
                }

                orderBoxList = orderBoxList.GroupBy(x => new
                {
                    x.BoxId,
                    x.OrderDetails
                }).Select(x => new ShipperOrderBoxResponse
                {
                    BoxId = x.Key.BoxId,
                    OrderDetails = x.Key.OrderDetails
                }).ToList();

                return new BaseResponsePagingViewModel<ShipperOrderBoxResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Total = orderBoxList.Count()
                    },
                    Data = orderBoxList
                };
            }
            catch(ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}
