using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Order;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.ErrorEnum;
using static FINE.Service.Helpers.Enum;
using Castle.Core.Resource;
using System.Net.Mail;
using IronBarCode;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Hangfire.Server;
using Microsoft.Extensions.Configuration;
using Hangfire;
using FINE.Service.Attributes;

namespace FINE.Service.Service
{
    public interface IOrderService
    {
        //Task<BaseResponseViewModel<GenOrderResponse>> GetOrderById(string id);
        //Task<BaseResponsePagingViewModel<GenOrderResponse>> GetOrders(PagingRequest paging);
        Task<BaseResponsePagingViewModel<OrderResponse>> GetOrderByCustomerId(string id, PagingRequest paging);
        Task<BaseResponseViewModel<OrderResponse>> CreatePreOrder(string customerId, CreatePreOrderRequest request);
        Task<BaseResponseViewModel<OrderResponse>> CreateOrder(string id, CreateOrderRequest request);
        //Task<BaseResponseViewModel<dynamic>> CreateCoOrder(string id, CreateGenOrderRequest request);
        //Task<BaseResponseViewModel<dynamic>> UpdateOrder(string id, UpdateOrderTypeEnum orderStatus, UpdateOrderRequest request);
    }
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        //private readonly INotifyService _notifyService;
        private readonly IConfiguration _configuration;

        public OrderService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration /*INotifyService notifyService*/)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;
            //_notifyService = notifyService;
        }

        public async Task<BaseResponsePagingViewModel<OrderResponse>> GetOrderByCustomerId(string customerId, PagingRequest paging)
        {
            try
            {
                var order = _unitOfWork.Repository<Order>().GetAll()
                                        .Where(x => x.CustomerId == Guid.Parse(customerId))
                                        .OrderByDescending(x => x.CheckInDate)
                                        .ProjectTo<OrderResponse>(_mapper.ConfigurationProvider)
                                        .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging,
                                        Constants.DefaultPaging);

                return new BaseResponsePagingViewModel<OrderResponse>()
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
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<OrderResponse>> CreatePreOrder(string customerId, CreatePreOrderRequest request)
        {
            try
            {
                #region check timeslot
                var timeSlot = _unitOfWork.Repository<TimeSlot>().Find(x => x.Id == request.TimeSlotId);

                if (timeSlot == null || timeSlot.IsActive == false)
                    throw new ErrorResponse(404, (int)TimeSlotErrorEnums.TIMESLOT_UNAVAILIABLE,
                        TimeSlotErrorEnums.TIMESLOT_UNAVAILIABLE.GetDisplayName());

                if (!Utils.CheckTimeSlot(timeSlot))
                    throw new ErrorResponse(400, (int)TimeSlotErrorEnums.OUT_OF_TIMESLOT,
                        TimeSlotErrorEnums.OUT_OF_TIMESLOT.GetDisplayName());
                #endregion

                var order = new OrderResponse()
                {
                    OrderCode = DateTime.Now.ToString("ddMMyy") + "-" + customerId,
                    OrderStatus = (int)OrderStatusEnum.PreOrder,
                    OrderType = request.OrderType,
                    TimeSlot = _mapper.Map<TimeSlotOrderResponse>(timeSlot),
                    StationOrder = null,
                    IsConfirm = false,
                    IsPartyMode = false
                };
                order.Customer = await _unitOfWork.Repository<Customer>().GetAll()
                                            .Where(x => x.Id ==Guid.Parse(customerId))
                                            .ProjectTo<CustomerOrderResponse>(_mapper.ConfigurationProvider)
                                            .FirstOrDefaultAsync();

                order.OrderDetails = new List<OrderDetailResponse>();
                foreach (var orderDetail in request.OrderDetails)
                {
                    var productInMenu = _unitOfWork.Repository<ProductInMenu>().GetAll()
                        .Include(x => x.Menu)
                        .Include(x => x.Product)
                        .Where(x => x.ProductId == orderDetail.ProductId && x.Menu.TimeSlotId == timeSlot.Id)
                        .FirstOrDefault();

                    if(productInMenu == null)
                    {
                        throw new ErrorResponse(404, (int)ProductInMenuErrorEnums.NOT_FOUND,
                           ProductInMenuErrorEnums.NOT_FOUND.GetDisplayName());
                    }
                    else if (timeSlot.Menus.FirstOrDefault(x => x.Id == productInMenu.MenuId) == null)
                    {
                        throw new ErrorResponse(404, (int)MenuErrorEnums.NOT_FOUND_MENU_IN_TIMESLOT,
                           MenuErrorEnums.NOT_FOUND_MENU_IN_TIMESLOT.GetDisplayName());
                    }
                    else if (productInMenu.IsActive == false || productInMenu.Status != (int)ProductInMenuStatusEnum.Avaliable)
                    {
                        throw new ErrorResponse(400, (int)ProductInMenuErrorEnums.PRODUCT_NOT_AVALIABLE,
                           ProductInMenuErrorEnums.PRODUCT_NOT_AVALIABLE.GetDisplayName());
                    }

                    var detail = new OrderDetailResponse()
                    {
                        ProductId = productInMenu.ProductId,
                        ProductName = productInMenu.Product.Name,
                        UnitPrice = productInMenu.Product.Price,
                        Quantity = orderDetail.Quantity,
                        TotalAmount = (double)(productInMenu.Product.Price * orderDetail.Quantity)
                    };
                    order.OrderDetails.Add(detail);
                    order.ItemQuantity += detail.Quantity;
                    order.TotalAmount += detail.TotalAmount;
                }

                var otherAmount = new OrderOtherAmount()
                {
                    Amount = 15000,
                    AmountType = (int)OtherAmountTypeEnum.ShippingFee
                };
                order.OtherAmounts = new List<OrderOtherAmount>();
                order.OtherAmounts.Add(otherAmount);
                order.TotalOtherAmount += otherAmount.Amount;
                order.FinalAmount = order.TotalAmount + order.TotalOtherAmount;
                order.Point = (int)(order.FinalAmount / 10000);

                return new BaseResponseViewModel<OrderResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = order
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<OrderResponse>> CreateOrder(string customerId, CreateOrderRequest request)
        {
            try
            {
                #region Check data
                var timeSlot = _unitOfWork.Repository<TimeSlot>().Find(x => x.Id == request.TimeSlotId);

                if (timeSlot == null || timeSlot.IsActive == false)
                    throw new ErrorResponse(404, (int)TimeSlotErrorEnums.TIMESLOT_UNAVAILIABLE,
                        TimeSlotErrorEnums.TIMESLOT_UNAVAILIABLE.GetDisplayName());

                if (!Utils.CheckTimeSlot(timeSlot))
                    throw new ErrorResponse(400, (int)TimeSlotErrorEnums.OUT_OF_TIMESLOT,
                        TimeSlotErrorEnums.OUT_OF_TIMESLOT.GetDisplayName());

                var customer = _unitOfWork.Repository<Customer>().GetAll()
                                        .FirstOrDefault(x => x.Id == Guid.Parse(customerId));
                if (customer.Phone == null)
                    throw new ErrorResponse(400, (int)CustomerErrorEnums.MISSING_PHONENUMBER,
                                            CustomerErrorEnums.MISSING_PHONENUMBER.GetDisplayName());

                var station = _unitOfWork.Repository<Station>().GetAll()
                                        .FirstOrDefault(x => x.Id == Guid.Parse(customerId));
                #endregion

                var order = _mapper.Map<Order>(request);
                order.Id = Guid.NewGuid();
                order.CustomerId = Guid.Parse(customerId);
                order.CheckInDate = DateTime.Now;
                order.OrderStatus = (int)OrderStatusEnum.PaymentPending;


                await _unitOfWork.Repository<Order>().InsertAsync(order);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<OrderResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<OrderResponse>(order)
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        //    public async Task<BaseResponseViewModel<GenOrderResponse>> GetOrderById(int orderId)
        //    {
        //        try
        //        {
        //            var order = await _unitOfWork.Repository<Order>().GetAll()
        //                .FirstOrDefaultAsync(x => x.Id == orderId);
        //            return new BaseResponseViewModel<GenOrderResponse>()
        //            {
        //                Status = new StatusViewModel()
        //                {
        //                    Message = "Success",
        //                    Success = true,
        //                    ErrorCode = 0
        //                },
        //                Data = _mapper.Map<GenOrderResponse>(order)
        //            };
        //        }
        //        catch (Exception ex)
        //        {
        //            throw ex;
        //        }
        //    }

        //    public async Task<BaseResponsePagingViewModel<GenOrderResponse>> GetOrders(PagingRequest paging)
        //    {
        //        try
        //        {
        //            var order = _unitOfWork.Repository<Order>().GetAll()
        //                                    .OrderByDescending(x => x.CheckInDate)
        //                                    .ProjectTo<GenOrderResponse>(_mapper.ConfigurationProvider)
        //                                    .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging,
        //                                    Constants.DefaultPaging);

        //            return new BaseResponsePagingViewModel<GenOrderResponse>()
        //            {
        //                Metadata = new PagingsMetadata()
        //                {
        //                    Page = paging.Page,
        //                    Size = paging.PageSize,
        //                    Total = order.Item1
        //                },
        //                Data = order.Item2.ToList()
        //            };
        //        }
        //        catch (Exception ex)
        //        {
        //            throw ex;
        //        }
        //    }

        //    public async Task CancelOrder(int orderId)
        //    {
        //        try
        //        {
        //            var order = await _unitOfWork.Repository<Order>().GetAll()
        //                .FirstOrDefaultAsync(x => x.Id == orderId);

        //            if (order == null)
        //                throw new ErrorResponse(404, (int)OrderErrorEnums.NOT_FOUND_ID,
        //                      OrderErrorEnums.NOT_FOUND_ID.GetDisplayName());

        //            if (order.GeneralOrderId != null)
        //            {
        //                var genOrder = await _unitOfWork.Repository<Order>().GetAll()
        //                .FirstOrDefaultAsync(x => x.Id == order.GeneralOrderId);

        //                if (genOrder.InverseGeneralOrder.Count() > 1)
        //                {
        //                    genOrder.TotalAmount -= order.TotalAmount;
        //                    genOrder.Discount -= order.Discount;
        //                    genOrder.FinalAmount -= order.FinalAmount;
        //                    genOrder.ShippingFee -= 2000;
        //                }
        //                else
        //                {
        //                    genOrder.OrderStatus = (int)OrderStatusEnum.UserCancel;
        //                }
        //                await _unitOfWork.Repository<Order>().UpdateDetached(genOrder);
        //                await _unitOfWork.CommitAsync();
        //            }
        //            else
        //            {
        //                foreach (var inverseOrder in order.InverseGeneralOrder)
        //                {
        //                    inverseOrder.OrderStatus = (int)OrderStatusEnum.UserCancel;
        //                }
        //            }
        //            order.OrderStatus = (int)OrderStatusEnum.UserCancel;

        //            await _unitOfWork.Repository<Order>().UpdateDetached(order);
        //            await _unitOfWork.CommitAsync();
        //        }
        //        catch (ErrorResponse ex)
        //        {
        //            throw ex;
        //        }
        //    }

        //    public async Task<BaseResponseViewModel<dynamic>> UpdateOrder(int orderId, UpdateOrderTypeEnum orderStatus, UpdateOrderRequest request)
        //    {
        //        try
        //        {
        //            var order = await _unitOfWork.Repository<Order>().GetAll()
        //                .FirstOrDefaultAsync(x => x.Id == orderId);

        //            if (order == null)
        //                throw new ErrorResponse(404, (int)OrderErrorEnums.NOT_FOUND_ID,
        //                          OrderErrorEnums.NOT_FOUND_ID.GetDisplayName());

        //            switch (orderStatus)
        //            {
        //                case UpdateOrderTypeEnum.UserCancel:

        //                    if (order.OrderStatus != (int)OrderStatusEnum.PaymentPending
        //                        || order.OrderStatus != (int)OrderStatusEnum.Processing)
        //                        throw new ErrorResponse(400, (int)OrderErrorEnums.CANNOT_CANCEL_ORDER,
        //                          OrderErrorEnums.CANNOT_CANCEL_ORDER.GetDisplayName());

        //                    CancelOrder(orderId);
        //                    break;

        //                case UpdateOrderTypeEnum.FinishOrder:

        //                    if (order.OrderStatus != (int)OrderStatusEnum.Delivering)
        //                        throw new ErrorResponse(400, (int)OrderErrorEnums.CANNOT_FINISH_ORDER,
        //                          OrderErrorEnums.CANNOT_FINISH_ORDER.GetDisplayName());

        //                    order.OrderStatus = (int)OrderStatusEnum.Finished;

        //                    await _unitOfWork.Repository<Order>().UpdateDetached(order);
        //                    await _unitOfWork.CommitAsync();
        //                    break;

        //                case UpdateOrderTypeEnum.UpdateDetails:

        //                    if (order.OrderStatus == (int)OrderStatusEnum.Finished)
        //                        throw new ErrorResponse(400, (int)OrderErrorEnums.CANNOT_CANCEL_ORDER,
        //                          OrderErrorEnums.CANNOT_CANCEL_ORDER.GetDisplayName());

        //                    if (request.DeliveryPhone != null && !Utils.CheckVNPhone(request.DeliveryPhone))
        //                        throw new ErrorResponse(400, (int)OrderErrorEnums.INVALID_PHONE_NUMBER,
        //                                                OrderErrorEnums.INVALID_PHONE_NUMBER.GetDisplayName());

        //                    var updateOrder = _mapper.Map<UpdateOrderRequest, Order>(request, order);
        //                    updateOrder.UpdateAt = DateTime.Now;
        //                    await _unitOfWork.Repository<Order>().UpdateDetached(updateOrder);
        //                    break;
        //            }

        //            return new BaseResponseViewModel<dynamic>()
        //            {
        //                Status = new StatusViewModel()
        //                {
        //                    Message = "Success",
        //                    Success = true,
        //                    ErrorCode = 0
        //                }
        //            };
        //        }
        //        catch (ErrorResponse ex)
        //        {
        //            throw ex;
        //        }
        //    }

        //    public async Task<BaseResponseViewModel<dynamic>> CreateCoOrder(int customerId, CreateGenOrderRequest request)
        //    {
        //        try
        //        {
        //            #region Timeslot
        //            var timeSlot = _unitOfWork.Repository<TimeSlot>().Find(x => x.Id == request.TimeSlotId);

        //            if (timeSlot == null)
        //                throw new ErrorResponse(404, (int)TimeSlotErrorEnums.NOT_FOUND,
        //                    TimeSlotErrorEnums.NOT_FOUND.GetDisplayName());

        //            if (!Utils.CheckTimeSlot(timeSlot))
        //                throw new ErrorResponse(400, (int)TimeSlotErrorEnums.OUT_OF_TIMESLOT,
        //                    TimeSlotErrorEnums.OUT_OF_TIMESLOT.GetDisplayName());
        //            #endregion

        //            #region Customer
        //            var customer = await _unitOfWork.Repository<Customer>().GetById(customerId);

        //            //check phone number
        //            if (customer.Phone.Contains(request.DeliveryPhone) == null)
        //            {
        //                if (!Utils.CheckVNPhone(request.DeliveryPhone))
        //                    throw new ErrorResponse(400, (int)OrderErrorEnums.INVALID_PHONE_NUMBER,
        //                                            OrderErrorEnums.INVALID_PHONE_NUMBER.GetDisplayName());
        //            }
        //            if (!request.DeliveryPhone.StartsWith("+84"))
        //            {
        //                request.DeliveryPhone = request.DeliveryPhone.TrimStart(new char[] { '0' });
        //                request.DeliveryPhone = "+84" + request.DeliveryPhone;
        //            }
        //            #endregion

        //            return null;
        //        }
        //        catch (ErrorResponse ex)
        //        {
        //            throw ex;
        //        }
        //    }
        //}
    }
}

