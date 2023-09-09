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
        Task<BaseResponsePagingViewModel<OrderDetailResponse>> GetOrdersDetailByStore(string storeId, PagingRequest paging);
        Task<BaseResponsePagingViewModel<OrderByStoreResponse>> GetStaffOrderDetail(string storeId);
        Task<BaseResponseViewModel<OrderByStoreResponse>> UpdateOrderByStoreStatus(List<UpdateOrderDetailStatusRequest> request);
        Task<BaseResponsePagingViewModel<OrderByStoreResponse>> GetStaffOrderDetailByOrderId(string orderId);

    }

    public class OrderDetailService : IOrderDetailService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public OrderDetailService(IUnitOfWork unitOfWork, IMapper mapper)
        private readonly IStaffService _staffService;
        public OrderDetailService(IUnitOfWork unitOfWork, IMapper mapper, IStaffService staffService)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _staffService = staffService;
        }

        public async Task<BaseResponsePagingViewModel<OrderDetailResponse>> GetOrdersDetailByStore(string storeId, PagingRequest paging)
        {
            try
            {
                var order = _unitOfWork.Repository<OrderDetail>().GetAll()
                                        .Where(x => x.StoreId == Guid.Parse(storeId))
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

        public async Task<BaseResponsePagingViewModel<OrderByStoreResponse>> GetStaffOrderDetail(string storeId)
        {
            try
            {
                // Get from Redis
                List<OrderByStoreResponse> orderResponse = await ServiceHelpers.GetSetDataRedisOrder(RedisSetUpType.GET);
                orderResponse = orderResponse.Where(x => x.StoreId == Guid.Parse(storeId))
                                             .OrderByDescending(x => x.CheckInDate)
                                             .ToList();

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

        public async Task<BaseResponseViewModel<OrderByStoreResponse>> UpdateOrderByStoreStatus(List<UpdateOrderDetailStatusRequest> request)
        {
            foreach (var item in request)
            {
                List<OrderByStoreResponse> orderResponse = await ServiceHelpers.GetSetDataRedisOrder(RedisSetUpType.GET, item.OrderId.ToString());
                orderResponse = orderResponse.OrderByDescending(x => x.CheckInDate)
                                                 .ToList();
                var order = orderResponse.FirstOrDefault(x => x.OrderId == item.OrderId && x.StoreId == item.StoreId);
                order.OrderDetailStoreStatus = item.OrderDetailStoreStatus;
                ServiceHelpers.GetSetDataRedisOrder(RedisSetUpType.SET, order.OrderId.ToString(), orderResponse);

                #region check Order status and update
                List<OrderByStoreResponse> checkOrderStatus = await ServiceHelpers.GetSetDataRedisOrder(RedisSetUpType.GET, item.OrderId.ToString());
                if( !checkOrderStatus.Any(x => x.OrderDetailStoreStatus != OrderStatusEnum.StaffConfirm))
                {
                    //no d co sai, t moi chay cho m coi y
                    var updateOrderStatusRequest = new UpdateOrderStatusRequest()
                    { 
                        OrderStatus = OrderStatusEnum.StaffConfirm
                    };
                    var updateOrder = await _staffService.UpdateOrderStatus(order.OrderId.ToString(), updateOrderStatusRequest);
                }
                else if (!checkOrderStatus.Any(x => x.OrderDetailStoreStatus != OrderStatusEnum.FinishPrepare))
                {
                    var updateOrderStatusRequest = new UpdateOrderStatusRequest()
                    {
                        OrderStatus = OrderStatusEnum.FinishPrepare
                    };
                    var updateOrder = await _staffService.UpdateOrderStatus(order.OrderId.ToString(), updateOrderStatusRequest);
                }
                else if (!checkOrderStatus.Any(x => x.OrderDetailStoreStatus != OrderStatusEnum.ShipperAssigned))
                {
                    var updateOrderStatusRequest = new UpdateOrderStatusRequest()
                    {
                        OrderStatus = OrderStatusEnum.ShipperAssigned
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
                #endregion
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
    }
}
