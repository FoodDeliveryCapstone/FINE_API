﻿using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Order;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using static FINE.Service.Helpers.ErrorEnum;
using static FINE.Service.Helpers.Enum;
using Microsoft.Extensions.Configuration;
using FINE.Service.Attributes;
using FINE.Service.Helpers;
using Hangfire;
using FirebaseAdmin.Messaging;
using Azure;
using System.IO;
using Castle.Core.Resource;

namespace FINE.Service.Service
{
    public interface IOrderService
    {
        Task<BaseResponseViewModel<OrderResponse>> GetOrderById(string customerId, string orderId);
        Task<BaseResponsePagingViewModel<OrderForStaffResponse>> GetOrders(OrderForStaffResponse filter, PagingRequest paging);
        Task<BaseResponsePagingViewModel<OrderResponseForCustomer>> GetOrderByCustomerId(string customerId, OrderResponseForCustomer filter, PagingRequest paging);
        Task<BaseResponseViewModel<dynamic>> GetOrderStatus(string orderId);
        Task<BaseResponseViewModel<CoOrderResponse>> GetPartyOrder(string customerId,string partyCode);
        Task<BaseResponseViewModel<OrderResponse>> CreatePreOrder(string customerId, CreatePreOrderRequest request);
        Task<BaseResponseViewModel<OrderResponse>> CreateOrder(string customerId, CreateOrderRequest request);
        Task<BaseResponseViewModel<CoOrderResponse>> OpenCoOrder(string customerId, CreatePreOrderRequest request);
        Task<BaseResponseViewModel<CoOrderResponse>> JoinPartyOrder(string customerId, string partyCode);
        Task<BaseResponseViewModel<AddProductToCardResponse>> AddProductToCard(AddProductToCardRequest request);
        Task<BaseResponseViewModel<CoOrderResponse>> AddProductIntoPartyCode(string customerId, string partyCode, CreatePreOrderRequest request);
        Task<BaseResponseViewModel<CoOrderPartyCard>> FinalConfirmCoOrder(string customerId, string partyCode);
        Task<BaseResponseViewModel<OrderResponse>> CreatePreCoOrder(string customerId, OrderTypeEnum orderType, string partyCode);
        Task<BaseResponseViewModel<CoOrderResponse>> DeletePartyOrder(string customerId, string partyCode, string memberId = null);
    }
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IPaymentService _paymentService;
        private readonly IConfiguration _configuration;
        private readonly INotifyService _notifyService;
        private readonly IFirebaseMessagingService _fm;

        public OrderService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, IPaymentService paymentService, INotifyService notifyService, IFirebaseMessagingService fm)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;
            _paymentService = paymentService;
            _notifyService = notifyService;
            _fm = fm;
        }

        public async Task<BaseResponsePagingViewModel<OrderResponseForCustomer>> GetOrderByCustomerId(string customerId, OrderResponseForCustomer filter, PagingRequest paging)
        {
            try
            {
                var customer = await _unitOfWork.Repository<Customer>().GetAll()
                                            .Where(y => y.Id == Guid.Parse(customerId))
                                            .ProjectTo<CustomerOrderResponse>(_mapper.ConfigurationProvider).FirstOrDefaultAsync();

                var order = _unitOfWork.Repository<Data.Entity.Order>().GetAll()
                                        .Where(x => x.CustomerId == Guid.Parse(customerId))
                                        .OrderByDescending(x => x.CheckInDate)
                                        .ProjectTo<OrderResponseForCustomer>(_mapper.ConfigurationProvider)
                                        .DynamicFilter(filter)
                                        .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging, Constants.DefaultPaging);


                return new BaseResponsePagingViewModel<OrderResponseForCustomer>()
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

        public async Task<BaseResponseViewModel<OrderResponse>> GetOrderById(string customerId, string id)
        {
            try
            {
                var order = await _unitOfWork.Repository<Data.Entity.Order>().GetAll()
                                    .FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

                var resultOrder = _mapper.Map<OrderResponse>(order);

                resultOrder.Customer = _unitOfWork.Repository<Customer>().GetAll()
                                        .Where(x => x.Id == Guid.Parse(customerId))
                                        .ProjectTo<CustomerOrderResponse>(_mapper.ConfigurationProvider)
                                        .FirstOrDefault();

                resultOrder.StationOrder = _unitOfWork.Repository<Station>().GetAll()
                                        .Where(x => x.Id == order.StationId)
                                        .ProjectTo<StationOrderResponse>(_mapper.ConfigurationProvider)
                                        .FirstOrDefault();

                var orderBox = await _unitOfWork.Repository<OrderBox>().GetAll()
                                    .Where(x => x.OrderId == order.Id)
                                    .FirstOrDefaultAsync();
                if (orderBox is not null)
                    resultOrder.BoxId = orderBox.BoxId;

                return new BaseResponseViewModel<OrderResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = resultOrder
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<dynamic>> GetOrderStatus(string orderId)
        {
            try
            {
                var order = await _unitOfWork.Repository<Order>().GetAll()
                                        .Where(x => x.Id == Guid.Parse(orderId))
                                        .FirstOrDefaultAsync();
                var result = new
                {
                    OrderStatus = order.OrderStatus,
                    BoxId = await _unitOfWork.Repository<OrderBox>().GetAll()
                                        .Where(x => x.OrderId == Guid.Parse(orderId))
                                        .Select(x => x.BoxId)
                                        .FirstOrDefaultAsync(),

                    StationName = await _unitOfWork.Repository<Station>().GetAll()
                                        .Where(x => x.Id == order.StationId)
                                        .Select(x => x.Name)
                                        .FirstOrDefaultAsync(),
                };

                return new BaseResponseViewModel<dynamic>()
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

                if (request.OrderType == OrderTypeEnum.OrderToday && !Utils.CheckTimeSlot(timeSlot))
                    throw new ErrorResponse(400, (int)TimeSlotErrorEnums.OUT_OF_TIMESLOT,
                        TimeSlotErrorEnums.OUT_OF_TIMESLOT.GetDisplayName());
                #endregion

                var order = new OrderResponse()
                {
                    Id = Guid.NewGuid(),
                    OrderCode = DateTime.Now.ToString("ddMM_HHmm") + "-" + Utils.GenerateRandomCode(4),
                    OrderStatus = (int)OrderStatusEnum.PreOrder,
                    OrderType = (int)request.OrderType,
                    TimeSlot = _mapper.Map<TimeSlotOrderResponse>(timeSlot),
                    StationOrder = null,
                    IsConfirm = false,
                    IsPartyMode = false
                };
                order.Customer = await _unitOfWork.Repository<Customer>().GetAll()
                                            .Where(x => x.Id == Guid.Parse(customerId))
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

                    if (productInMenu == null)
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
                        Id = Guid.NewGuid(),
                        OrderId = (Guid)order.Id,
                        ProductId = productInMenu.ProductId,
                        ProductInMenuId = productInMenu.Id,
                        StoreId = productInMenu.Product.Product.StoreId,
                        ProductName = productInMenu.Product.Name,
                        ProductCode = productInMenu.Product.Code,
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
                    Id = Guid.NewGuid(),
                    OrderId = (Guid)order.Id,
                    Amount = 15000,
                    Type = (int)OtherAmountTypeEnum.ShippingFee
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
                var timeSlot = await _unitOfWork.Repository<TimeSlot>().FindAsync(x => x.Id == request.TimeSlotId);

                if (request.OrderType is OrderTypeEnum.OrderToday && !Utils.CheckTimeSlot(timeSlot))
                    throw new ErrorResponse(400, (int)TimeSlotErrorEnums.OUT_OF_TIMESLOT,
                        TimeSlotErrorEnums.OUT_OF_TIMESLOT.GetDisplayName());

                var customer = await _unitOfWork.Repository<Customer>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(customerId));

                if (customer.Phone is null)
                    throw new ErrorResponse(400, (int)CustomerErrorEnums.MISSING_PHONENUMBER,
                                            CustomerErrorEnums.MISSING_PHONENUMBER.GetDisplayName());

                var station = await _unitOfWork.Repository<Station>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(request.StationId));

                var order = _mapper.Map<Data.Entity.Order>(request);
                order.CustomerId = Guid.Parse(customerId);
                order.CheckInDate = DateTime.Now;
                order.OrderStatus = (int)OrderStatusEnum.PaymentPending;

                if (request.PartyCode is not null)
                {
                    var checkCode = await _unitOfWork.Repository<Party>().GetAll()
                                    .Where(x => x.PartyCode == request.PartyCode)
                                    .ToListAsync();

                    if (checkCode == null)
                        throw new ErrorResponse(400, (int)PartyErrorEnums.INVALID_CODE, PartyErrorEnums.INVALID_CODE.GetDisplayName());

                    if (checkCode.Any(x => x.Status == (int)PartyOrderStatus.CloseParty))
                        throw new ErrorResponse(404, (int)PartyErrorEnums.PARTY_CLOSED, PartyErrorEnums.PARTY_CLOSED.GetDisplayName());

                    var checkJoin = checkCode.Find(x => x.CustomerId == Guid.Parse(customerId));
                    if (checkJoin == null)
                    {
                        var linkedOrder = new Party()
                        {
                            Id = Guid.NewGuid(),
                            OrderId = request.Id,
                            CustomerId = Guid.Parse(customerId),
                            PartyCode = request.PartyCode,
                            PartyType = (int)PartyOrderType.LinkedOrder,
                            Status = (int)PartyOrderStatus.Confirm,
                            IsActive = true,
                            CreateAt = DateTime.Now
                        };
                        order.Parties.Add(linkedOrder);
                    }
                    else
                    {
                        checkJoin.OrderId = request.Id;
                        checkJoin.Status = (int)PartyOrderStatus.Confirm;
                        await _unitOfWork.Repository<Party>().UpdateDetached(checkJoin);
                    }
                }

                await _unitOfWork.Repository<Data.Entity.Order>().InsertAsync(order);
                await _unitOfWork.CommitAsync();

                await _paymentService.CreatePayment(order, request.Point, request.PaymentType);

                order.OrderStatus = (int)OrderStatusEnum.Processing;

                await _unitOfWork.Repository<Data.Entity.Order>().UpdateDetached(order);
                await _unitOfWork.CommitAsync();

                var resultOrder = _mapper.Map<OrderResponse>(order);
                resultOrder.Customer = _mapper.Map<CustomerOrderResponse>(customer);
                resultOrder.StationOrder = _unitOfWork.Repository<Station>().GetAll()
                                                .Where(x => x.Id == Guid.Parse(request.StationId))
                                                .ProjectTo<StationOrderResponse>(_mapper.ConfigurationProvider)
                                                .FirstOrDefault();

                #region Background Job
                NotifyOrderRequestModel notifyRequest = new NotifyOrderRequestModel
                {
                    OrderCode = order.OrderCode,
                    CustomerId = customer.Id,
                    OrderStatus = (OrderStatusEnum?)order.OrderStatus
                };

                var notify = _notifyService.CreateOrderNotify(notifyRequest).Result;

                var customerToken = _unitOfWork.Repository<Fcmtoken>().GetAll().FirstOrDefault(x => x.UserId == customer.Id).Token;

                Notification notification = new Notification
                {
                    Title = Constants.SUC_ORDER_CREATED,
                    Body = String.Format("Đơn hàng của bạn đã được đặt thành công !!!")
                };

                var data = new Dictionary<string, string>()
                    {
                        { "type", NotifyTypeEnum.ForUsual.ToString()}
                    };

                BackgroundJob.Enqueue(() => _fm.SendToToken(customerToken, notification, data));

                var party = _unitOfWork.Repository<Party>().GetAll().FirstOrDefault(x => x.OrderId == order.Id);

                if (party.PartyType == (int)PartyOrderType.CoOrder)
                {
                    CoOrderResponse coOrder = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, party.PartyCode);
                    coOrder.IsPayment = true;
                    await ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, party.PartyCode);
                }
                #endregion

                return new BaseResponseViewModel<OrderResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = resultOrder
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<CoOrderResponse>> GetPartyOrder(string customerId ,string partyCode)
        {
            try
            {
                var partyOrder = await _unitOfWork.Repository<Party>().GetAll()
                                                .Where(x => x.PartyCode == partyCode && x.CustomerId == Guid.Parse(customerId))
                                                .FirstOrDefaultAsync();
                if (partyOrder == null)
                    throw new ErrorResponse(400, (int)PartyErrorEnums.INVALID_CODE, PartyErrorEnums.INVALID_CODE.GetDisplayName());

                if (partyOrder.IsActive == false)
                    throw new ErrorResponse(404, (int)PartyErrorEnums.PARTY_DELETE, PartyErrorEnums.PARTY_DELETE.GetDisplayName());

                if (partyOrder.Status == (int)PartyOrderStatus.OutOfTimeslot)
                    throw new ErrorResponse(404, (int)PartyErrorEnums.OUT_OF_TIMESLOT, PartyErrorEnums.OUT_OF_TIMESLOT.GetDisplayName());
                
                CoOrderResponse coOrder = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, partyCode);

                return new BaseResponseViewModel<CoOrderResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = coOrder
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<CoOrderResponse>> OpenCoOrder(string customerId, CreatePreOrderRequest request)
        {
            try
            {
                #region check timeslot
                var timeSlot = _unitOfWork.Repository<TimeSlot>().Find(x => x.Id == request.TimeSlotId);

                if (timeSlot is null || timeSlot.IsActive is false)
                    throw new ErrorResponse(404, (int)TimeSlotErrorEnums.TIMESLOT_UNAVAILIABLE,
                        TimeSlotErrorEnums.TIMESLOT_UNAVAILIABLE.GetDisplayName());

                if (request.OrderType is OrderTypeEnum.OrderToday && !Utils.CheckTimeSlot(timeSlot))
                    throw new ErrorResponse(400, (int)TimeSlotErrorEnums.OUT_OF_TIMESLOT,
                        TimeSlotErrorEnums.OUT_OF_TIMESLOT.GetDisplayName());
                #endregion

                var customer = await _unitOfWork.Repository<Customer>().GetAll()
                                    .FirstOrDefaultAsync(x => x.Id == Guid.Parse(customerId));

                var coOrder = new CoOrderResponse()
                {
                    Id = Guid.NewGuid(),
                    PartyCode = Constants.PARTYORDER_LINKED + Utils.GenerateRandomCode(6),
                    PartyType = (int)PartyOrderType.LinkedOrder,
                    TimeSlot = _mapper.Map<TimeSlotOrderResponse>(timeSlot)
                };

                var party = new Party()
                {
                    Id = Guid.NewGuid(),
                    CustomerId = Guid.Parse(customerId),
                    PartyCode = coOrder.PartyCode,
                    PartyType = (int)PartyOrderType.LinkedOrder,
                    Status = (int)PartyOrderStatus.NotConfirm,
                    IsActive = true,
                    CreateAt = DateTime.Now
                };

                if (request.PartyType is PartyOrderType.CoOrder)
                {
                    var closingTimeSlot = timeSlot.ArriveTime.Add(new TimeSpan(0, -45, 0));

                    var currentTime = Utils.GetCurrentDatetime().TimeOfDay;

                    var timeWait = closingTimeSlot - currentTime;

                    BackgroundJob.Schedule(() => UpdatePartyOrderStatus(coOrder.PartyCode), TimeSpan.FromMinutes(timeWait.TotalMinutes));

                    party.PartyType = (int)PartyOrderType.CoOrder;
                    party.PartyCode = Constants.PARTYORDER_COLAB + Utils.GenerateRandomCode(6);

                    coOrder.PartyCode = party.PartyCode;
                    coOrder.IsPayment = false;
                    coOrder.OrderType = (int)request.OrderType;
                    coOrder.PartyType = (int)PartyOrderType.CoOrder;
                    coOrder.PartyOrder = new List<CoOrderPartyCard>();

                    var orderCard = new CoOrderPartyCard()
                    {
                        Customer = _mapper.Map<CustomerCoOrderResponse>(customer),
                        OrderDetails = new List<CoOrderDetailResponse>()
                    };

                    orderCard.Customer.IsAdmin = true;
                    orderCard.Customer.IsConfirm = true;

                    if (request.OrderDetails != null)
                    {
                        foreach (var orderDetail in request.OrderDetails)
                        {
                            var productInMenu = _unitOfWork.Repository<ProductInMenu>().GetAll()
                                .Include(x => x.Menu)
                                .Include(x => x.Product)
                                .Where(x => x.ProductId == orderDetail.ProductId && x.Menu.TimeSlotId == timeSlot.Id)
                                .FirstOrDefault();

                            if (productInMenu == null)
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

                            var product = new CoOrderDetailResponse()
                            {
                                ProductInMenuId = productInMenu.Id,
                                ProductId = productInMenu.ProductId,
                                ProductName = productInMenu.Product.Name,
                                UnitPrice = productInMenu.Product.Price,
                                Quantity = orderDetail.Quantity,
                                TotalAmount = orderDetail.Quantity * productInMenu.Product.Price,
                                Note = orderDetail.Note
                            };
                            orderCard.OrderDetails.Add(product);
                            orderCard.ItemQuantity += product.Quantity;
                            orderCard.TotalAmount += product.TotalAmount;
                        }
                    }
                    coOrder.PartyOrder.Add(orderCard);

                    ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, coOrder.PartyCode, coOrder);
                }

                await _unitOfWork.Repository<Party>().InsertAsync(party);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<CoOrderResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = coOrder

                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public void UpdatePartyOrderStatus(string code)
        {
            try
            {
                    var party = _unitOfWork.Repository<Party>().GetAll()
                                .Where(x => x.PartyCode == code).FirstOrDefault();

                    party.Status = (int)PartyOrderStatus.OutOfTimeslot;

                     _unitOfWork.Repository<Party>().UpdateDetached(party);
                     _unitOfWork.Commit();

                ServiceHelpers.GetSetDataRedis(RedisSetUpType.DELETE, code);
                }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<CoOrderResponse>> JoinPartyOrder(string customerId, string partyCode)
        {
            try
            {
                var listpartyOrder = await _unitOfWork.Repository<Party>().GetAll()
                                                .Where(x => x.PartyCode == partyCode)
                                                .ToListAsync();

                if (listpartyOrder.Any(x => x.CustomerId == Guid.Parse(customerId)))
                {
                    throw new ErrorResponse(400, (int)PartyErrorEnums.PARTY_JOINED, PartyErrorEnums.PARTY_JOINED.GetDisplayName());
                }
                else if (listpartyOrder.All(x => x.IsActive == false))
                {
                    throw new ErrorResponse(400, (int)PartyErrorEnums.PARTY_DELETE, PartyErrorEnums.PARTY_DELETE.GetDisplayName());
                }
                else if (listpartyOrder.Any(x => x.Status == (int)PartyOrderStatus.CloseParty))
                {
                    throw new ErrorResponse(400, (int)PartyErrorEnums.PARTY_CLOSED, PartyErrorEnums.PARTY_CLOSED.GetDisplayName());
                }
                else if(listpartyOrder == null)
                {
                    throw new ErrorResponse(400, (int)PartyErrorEnums.INVALID_CODE, PartyErrorEnums.INVALID_CODE.GetDisplayName());
                }

                var newParty = new Party()
                {
                    Id = Guid.NewGuid(),
                    CustomerId = Guid.Parse(customerId),
                    PartyCode = partyCode,
                    PartyType = (int)PartyOrderType.LinkedOrder,
                    Status = (int)PartyOrderStatus.NotConfirm,
                    IsActive = true,
                    CreateAt = DateTime.Now,
                };

                if (listpartyOrder.FirstOrDefault().PartyType is (int)PartyOrderType.CoOrder)
                {
                    CoOrderResponse coOrder = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, partyCode);

                    if (coOrder is null)
                        throw new ErrorResponse(400, (int)OrderErrorEnums.NOT_FOUND_COORDER, OrderErrorEnums.NOT_FOUND_COORDER.GetDisplayName());

                    var customer = await _unitOfWork.Repository<Customer>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(customerId));

                    var orderCard = new CoOrderPartyCard()
                    {
                        Customer = _mapper.Map<CustomerCoOrderResponse>(customer)
                    };
                    orderCard.Customer.IsConfirm = false;
                    coOrder.PartyOrder.Add(orderCard);

                    ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, partyCode, coOrder);

                    newParty.PartyType = (int)PartyOrderType.CoOrder;
                }
                _unitOfWork.Repository<Party>().InsertAsync(newParty);
                _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<CoOrderResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    }
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<AddProductToCardResponse>> AddProductToCard(AddProductToCardRequest request)
        {
            try
            {
                var existInCard = request.Card.Find(x => x.ProductId == request.ProductId);
                if (existInCard is not null)
                {
                    request.Quantity += existInCard.Quantity;
                    request.Card.Remove(existInCard);
                }

                var productRequest = await _unitOfWork.Repository<ProductInMenu>().GetAll()
                                        .Include(x => x.Product)
                                        .Where(x => x.ProductId == Guid.Parse(request.ProductId)
                                            && x.Menu.TimeSlotId == Guid.Parse(request.TimeSlotId))
                                        .GroupBy(x => x.Product)
                                        .Select(x => x.Key)
                                        .FirstOrDefaultAsync();

                var result = new AddProductToCardResponse
                {
                    Product = _mapper.Map<ProductInCardResponse>(productRequest),
                    Card = new List<ProductInCardResponse>(),
                    ProductsRecommend = new List<ProductRecommend>()
                };
                result.Product.Quantity = request.Quantity;

                List<CheckFixBoxRequest> listProductInCard = new List<CheckFixBoxRequest>();
                if (request.Card is not null)
                {
                    foreach (var product in request.Card)
                    {
                        var productAttInCard = _unitOfWork.Repository<ProductInMenu>().GetAll()
                                                        .Include(x => x.Product)
                                                        .Where(x => x.ProductId == Guid.Parse(product.ProductId)
                                                            && x.Menu.TimeSlotId == Guid.Parse(request.TimeSlotId))
                                                        .GroupBy(x => x.Product)
                                                        .AsQueryable();

                        var responseProduct = productAttInCard.Select(x => x.Key).ProjectTo<ProductInCardResponse>(_mapper.ConfigurationProvider).FirstOrDefaultAsync().Result;
                        responseProduct.Quantity = product.Quantity;
                        result.Card.Add(responseProduct);

                        CheckFixBoxRequest productRequestFill = productAttInCard.Select(x => new CheckFixBoxRequest()
                        {
                            Product = x.Key,
                            Quantity = product.Quantity
                        })
                        .FirstOrDefaultAsync().Result;

                        listProductInCard.Add(productRequestFill);
                    }
                }

                var addToBoxResult = ServiceHelpers.CheckProductFixTheBox(productRequest, request.Quantity, listProductInCard);

                if (addToBoxResult.QuantitySuccess == request.Quantity)
                {
                    result.Status = new StatusViewModel()
                    {
                        Success = true,
                        Message = "Success",
                        ErrorCode = 200
                    };

                    result.Card.Add(result.Product);
                }
                else if (addToBoxResult.QuantitySuccess < request.Quantity && addToBoxResult.QuantitySuccess != 0)
                {
                    var quantityCanAdd = addToBoxResult.QuantitySuccess;
                    result.Status = new StatusViewModel()
                    {
                        Success = true,
                        Message = String.Format($"Only {quantityCanAdd} items can be added to the card"),
                        ErrorCode = 2001
                    };
                    result.Product.Quantity = quantityCanAdd;
                    result.Card.Add(result.Product);
                }
                else
                {
                    result.Status = new StatusViewModel()
                    {
                        Success = false,
                        Message = String.Format("Error"),
                        ErrorCode = 400
                    };
                }

                var products = _unitOfWork.Repository<ProductInMenu>().GetAll()
                                                   .Include(x => x.Menu)
                                                   .Include(x => x.Product)
                                                   .Where(x => x.Menu.TimeSlotId == Guid.Parse(request.TimeSlotId))
                                                   .Select(x => x.Product)
                                                   .AsQueryable();

                result.ProductsRecommend = await products.Where(x => x.Height <= addToBoxResult.RemainingLengthSpace.Height
                                                             && x.Width <= addToBoxResult.RemainingLengthSpace.Width
                                                             && x.Length <= addToBoxResult.RemainingLengthSpace.Length)
                                                            .ProjectTo<ProductRecommend>(_mapper.ConfigurationProvider)
                                                            .ToListAsync();

                var listRecommendWidth = await products.Where(x => x.Height <= addToBoxResult.RemainingWidthSpace.Height
                                                             && x.Width <= addToBoxResult.RemainingWidthSpace.Width
                                                             && x.Length <= addToBoxResult.RemainingWidthSpace.Length)
                                                            .ProjectTo<ProductRecommend>(_mapper.ConfigurationProvider)
                                                            .ToListAsync();

                result.ProductsRecommend.AddRange(listRecommendWidth);

                return new BaseResponseViewModel<AddProductToCardResponse>()
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
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<CoOrderResponse>> AddProductIntoPartyCode(string customerId, string partyCode, CreatePreOrderRequest request)
        {
            try
            {
                CoOrderResponse coOrder = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, partyCode);

                if (coOrder is null)
                    throw new ErrorResponse(400, (int)OrderErrorEnums.NOT_FOUND_COORDER, OrderErrorEnums.NOT_FOUND_COORDER.GetDisplayName());

                #region check timeslot
                var timeSlot = _unitOfWork.Repository<TimeSlot>().Find(x => x.Id == request.TimeSlotId);

                if (timeSlot == null || timeSlot.IsActive == false)
                    throw new ErrorResponse(404, (int)TimeSlotErrorEnums.TIMESLOT_UNAVAILIABLE,
                        TimeSlotErrorEnums.TIMESLOT_UNAVAILIABLE.GetDisplayName());

                if (request.OrderType is OrderTypeEnum.OrderToday && !Utils.CheckTimeSlot(timeSlot))
                    throw new ErrorResponse(400, (int)TimeSlotErrorEnums.OUT_OF_TIMESLOT,
                        TimeSlotErrorEnums.OUT_OF_TIMESLOT.GetDisplayName());
                #endregion

                var partyOrder = await _unitOfWork.Repository<Party>().GetAll()
                                                .Where(x => x.PartyCode == partyCode)
                                                .FirstOrDefaultAsync();
                if (partyOrder == null)
                    throw new ErrorResponse(400, (int)PartyErrorEnums.INVALID_CODE, PartyErrorEnums.INVALID_CODE.GetDisplayName());

                var orderCard = coOrder.PartyOrder.FirstOrDefault(x => x.Customer.Id == Guid.Parse(customerId));
                if (orderCard == null)
                {
                    var customer = await _unitOfWork.Repository<Customer>().GetAll()
                            .Where(x => x.Id == Guid.Parse(customerId))
                            .FirstOrDefaultAsync();
                    orderCard = new CoOrderPartyCard()
                    {
                        Customer = _mapper.Map<CustomerCoOrderResponse>(customer),
                        OrderDetails = new List<CoOrderDetailResponse>()
                    };
                    coOrder.PartyOrder.Add(orderCard);
                }
                else
                {
                    orderCard.OrderDetails = new List<CoOrderDetailResponse>();
                    orderCard.ItemQuantity = 0;
                    orderCard.TotalAmount = 0;
                }

                foreach (var requestOD in request.OrderDetails)
                {
                    var productInMenu = _unitOfWork.Repository<ProductInMenu>().GetAll()
                        .Include(x => x.Menu)
                        .Include(x => x.Product)
                        .Where(x => x.ProductId == requestOD.ProductId && x.Menu.TimeSlotId == timeSlot.Id)
                        .FirstOrDefault();

                    if (productInMenu is null)
                        throw new ErrorResponse(404, (int)ProductInMenuErrorEnums.NOT_FOUND,
                           ProductInMenuErrorEnums.NOT_FOUND.GetDisplayName());

                    if (productInMenu.IsActive == false || productInMenu.Status != (int)ProductInMenuStatusEnum.Avaliable)
                        throw new ErrorResponse(400, (int)ProductInMenuErrorEnums.PRODUCT_NOT_AVALIABLE,
                           ProductInMenuErrorEnums.PRODUCT_NOT_AVALIABLE.GetDisplayName());

                    var product = new CoOrderDetailResponse()
                    {
                        ProductInMenuId = productInMenu.Id,
                        ProductId = productInMenu.ProductId,
                        ProductName = productInMenu.Product.Name,
                        UnitPrice = productInMenu.Product.Price,
                        Quantity = requestOD.Quantity,
                        TotalAmount = requestOD.Quantity * productInMenu.Product.Price,
                        Note = requestOD.Note
                    };

                    orderCard.OrderDetails.Add(product);
                    orderCard.ItemQuantity += product.Quantity;
                    orderCard.TotalAmount += product.TotalAmount;
                }

                ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, partyCode, coOrder);

                return new BaseResponseViewModel<CoOrderResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    }
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<CoOrderPartyCard>> FinalConfirmCoOrder(string customerId, string partyCode)
        {
            try
            {

                var partyOrder = await _unitOfWork.Repository<Party>().GetAll()
                                                .Where(x => x.PartyCode == partyCode && x.CustomerId == Guid.Parse(customerId))
                                                .FirstOrDefaultAsync();
                if (partyOrder == null)
                    throw new ErrorResponse(400, (int)PartyErrorEnums.INVALID_CODE, PartyErrorEnums.INVALID_CODE.GetDisplayName());

                partyOrder.Status = (int)PartyOrderStatus.Confirm;

                await _unitOfWork.Repository<Party>().UpdateDetached(partyOrder);
                await _unitOfWork.CommitAsync();

                CoOrderResponse coOrder = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, partyCode);

                if (coOrder is null)
                    throw new ErrorResponse(400, (int)OrderErrorEnums.NOT_FOUND_COORDER, OrderErrorEnums.NOT_FOUND_COORDER.GetDisplayName());

                var orderCard = coOrder.PartyOrder.FirstOrDefault(x => x.Customer.Id == Guid.Parse(customerId));
                orderCard.Customer.IsConfirm = true;

                var rs = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, partyCode, coOrder);

                return new BaseResponseViewModel<CoOrderPartyCard>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = orderCard
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<OrderResponse>> CreatePreCoOrder(string customerId, OrderTypeEnum orderType, string partyCode)
        {
            try
            {
                CoOrderResponse coOrder = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, partyCode);

                if (coOrder is null)
                    throw new ErrorResponse(400, (int)OrderErrorEnums.NOT_FOUND_COORDER, OrderErrorEnums.NOT_FOUND_COORDER.GetDisplayName());

                #region check timeslot
                var timeSlot = _unitOfWork.Repository<TimeSlot>().Find(x => x.Id == Guid.Parse(coOrder.TimeSlot.Id));

                if (timeSlot == null || timeSlot.IsActive == false)
                    throw new ErrorResponse(404, (int)TimeSlotErrorEnums.TIMESLOT_UNAVAILIABLE,
                        TimeSlotErrorEnums.TIMESLOT_UNAVAILIABLE.GetDisplayName());

                if (orderType is OrderTypeEnum.OrderToday && !Utils.CheckTimeSlot(timeSlot))
                    throw new ErrorResponse(400, (int)TimeSlotErrorEnums.OUT_OF_TIMESLOT,
                        TimeSlotErrorEnums.OUT_OF_TIMESLOT.GetDisplayName());
                #endregion

                var order = new OrderResponse()
                {
                    Id = Guid.NewGuid(),
                    OrderCode = DateTime.Now.ToString("ddMMyy_HHmm") + "-" + Utils.GenerateRandomCode(5) + "-" + customerId,
                    OrderStatus = (int)OrderStatusEnum.PreOrder,
                    OrderType = (int)OrderTypeEnum.OrderToday,
                    TimeSlot = _mapper.Map<TimeSlotOrderResponse>(timeSlot),
                    StationOrder = null,
                    IsConfirm = false,
                    IsPartyMode = true
                };

                order.Customer = await _unitOfWork.Repository<Customer>().GetAll()
                                            .Where(x => x.Id == Guid.Parse(customerId))
                                            .ProjectTo<CustomerOrderResponse>(_mapper.ConfigurationProvider)
                                            .FirstOrDefaultAsync();

                order.OrderDetails = new List<OrderDetailResponse>();
                foreach (var customerOrder in coOrder.PartyOrder)
                {
                    foreach (var orderDetail in customerOrder.OrderDetails)
                    {
                        var productInMenu = _unitOfWork.Repository<ProductInMenu>().GetAll()
                            .Include(x => x.Menu)
                            .Include(x => x.Product)
                            .Where(x => x.ProductId == orderDetail.ProductId && x.Menu.TimeSlotId == timeSlot.Id)
                            .FirstOrDefault();

                        if (productInMenu == null)
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
                        var product = order.OrderDetails.Find(x => x.ProductId == orderDetail.ProductId);
                        if (product == null)
                        {
                            var detail = new OrderDetailResponse()
                            {
                                Id = Guid.NewGuid(),
                                OrderId = (Guid)order.Id,
                                ProductId = productInMenu.ProductId,
                                ProductInMenuId = productInMenu.Id,
                                StoreId = productInMenu.Product.Product.StoreId,
                                ProductName = productInMenu.Product.Name,
                                ProductCode = productInMenu.Product.Code,
                                UnitPrice = productInMenu.Product.Price,
                                Quantity = orderDetail.Quantity,
                                TotalAmount = (double)(productInMenu.Product.Price * orderDetail.Quantity)
                            };
                            order.OrderDetails.Add(detail);
                            order.ItemQuantity += detail.Quantity;
                            order.TotalAmount += detail.TotalAmount;
                        }
                        else
                        {
                            product.Quantity += orderDetail.Quantity;
                            product.TotalAmount += orderDetail.TotalAmount;

                            order.ItemQuantity += product.Quantity;
                            order.TotalAmount += product.TotalAmount;
                        }
                    }
                }

                var otherAmount = new OrderOtherAmount()
                {
                    Id = Guid.NewGuid(),
                    OrderId = (Guid)order.Id,
                    Amount = 15000,
                    Type = (int)OtherAmountTypeEnum.ShippingFee
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

        public async Task<BaseResponseViewModel<CoOrderResponse>> DeletePartyOrder(string customerId, string partyCode, string memberId = null)
        {
            try
            {
                var listParty = await _unitOfWork.Repository<Party>().GetAll()
                                               .Where(x => x.PartyCode == partyCode)
                                               .ToListAsync();
                if (listParty is null)
                    throw new ErrorResponse(400, (int)PartyErrorEnums.INVALID_CODE, PartyErrorEnums.INVALID_CODE.GetDisplayName());

                CoOrderResponse coOrder = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, partyCode);

                if (coOrder is null)
                    throw new ErrorResponse(400, (int)OrderErrorEnums.NOT_FOUND_COORDER, OrderErrorEnums.NOT_FOUND_COORDER.GetDisplayName());

                var partyMem = coOrder.PartyOrder.Find(x => x.Customer.Id == Guid.Parse(customerId));

                // đứa delete là admin
                if (partyMem.Customer.IsAdmin is true)
                {
                    var party = listParty.FirstOrDefault(x => x.CustomerId == Guid.Parse(customerId));
                    party.IsActive = false;
                    await _unitOfWork.Repository<Party>().UpdateDetached(party);
                    //nếu đơn nhóm chỉ có admin => xóa chế độ coOrder
                    if (memberId is not null)
                    {
                        ServiceHelpers.GetSetDataRedis(RedisSetUpType.DELETE, partyCode);
                    }
                    else
                    {
                        coOrder.PartyOrder.FirstOrDefault(x => x.Customer.Id == Guid.Parse(memberId)).Customer.IsAdmin = true;
                        coOrder.PartyOrder.Remove(partyMem);
                        ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, partyCode, coOrder);

                        var customerToken = _unitOfWork.Repository<Fcmtoken>().GetAll().FirstOrDefault(x => x.UserId == Guid.Parse(memberId)).Token;

                        Notification notification = new Notification
                        {
                            Title = Constants.CHANGE_ADMIN_PARTY,
                            Body = String.Format($"Bạn vừa được {party.Customer.Name} chuyển quyền admin đơn nhóm {coOrder.PartyCode}")
                        };

                        var data = new Dictionary<string, string>()
                    {
                        { "type", NotifyTypeEnum.ForPopup.ToString()}
                    };

                        BackgroundJob.Enqueue(() => _fm.SendToToken(customerToken, notification, data));
                    }
                }
                else
                {
                    var party = listParty.FirstOrDefault(x => x.CustomerId == Guid.Parse(customerId));
                    party.IsActive = false;
                    await _unitOfWork.Repository<Party>().UpdateDetached(party);

                    coOrder.PartyOrder.Remove(partyMem);
                    ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, partyCode, coOrder);
                }

                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<CoOrderResponse>()
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

        public async Task<BaseResponsePagingViewModel<OrderForStaffResponse>> GetOrders(OrderForStaffResponse filter, PagingRequest paging)
        {
            try
            {

                var order = _unitOfWork.Repository<Data.Entity.Order>().GetAll()
                                        .OrderByDescending(x => x.CheckInDate)
                                        .ProjectTo<OrderForStaffResponse>(_mapper.ConfigurationProvider)
                                        .DynamicFilter(filter)
                                        .DynamicSort(filter)
                                        .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging,
                                        Constants.DefaultPaging);

                return new BaseResponsePagingViewModel<OrderForStaffResponse>()
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
    }
}