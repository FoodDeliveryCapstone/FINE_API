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
using System.Linq.Dynamic.Core;
using static FINE.Service.Helpers.ErrorEnum;
using static FINE.Service.Helpers.Enum;
using Microsoft.Extensions.Configuration;
using FINE.Service.Attributes;
using FINE.Service.Helpers;
using Hangfire;
using FirebaseAdmin.Messaging;
using Newtonsoft.Json;
using Microsoft.IdentityModel.Tokens;
using NetTopologySuite.Index.HPRtree;

namespace FINE.Service.Service
{
    public interface IOrderService
    {
        Task<BaseResponseViewModel<OrderResponse>> GetOrderById(string customerId, string orderId);
        Task<BaseResponsePagingViewModel<OrderForStaffResponse>> GetOrders(OrderForStaffResponse filter, PagingRequest paging);
        Task<BaseResponsePagingViewModel<OrderResponseForCustomer>> GetOrderByCustomerId(string customerId, OrderResponseForCustomer filter, PagingRequest paging);
        Task<BaseResponseViewModel<dynamic>> GetOrderStatus(string orderId);
        Task<BaseResponseViewModel<CoOrderResponse>> GetPartyOrder(string customerId, string partyCode);
        Task<BaseResponseViewModel<CoOrderStatusResponse>> GetPartyStatus(string partyCode);
        Task<BaseResponseViewModel<OrderResponse>> CreatePreOrder(string customerId, CreatePreOrderRequest request);
        Task<BaseResponseViewModel<CreateReOrderResponse>> CreatePreOrderFromReOrder(string customerId, string reOrderId, OrderTypeEnum orderType);
        Task<BaseResponseViewModel<OrderResponse>> CreateOrder(string customerId, CreateOrderRequest request);
        Task<BaseResponseViewModel<CoOrderResponse>> OpenParty(string customerId, CreatePreOrderRequest request);
        Task<BaseResponseViewModel<CoOrderResponse>> JoinPartyOrder(string customerId, string timeSlotId, string partyCode);
        Task<BaseResponseViewModel<AddProductToCardResponse>> AddProductToCard(string customerId, AddProductToCardRequest request);
        Task<BaseResponseViewModel<CoOrderResponse>> AddProductIntoPartyCode(string customerId, string partyCode, CreatePreOrderRequest request);
        Task<BaseResponseViewModel<CoOrderPartyCard>> FinalConfirmCoOrder(string customerId, string partyCode);
        Task<BaseResponseViewModel<OrderResponse>> CreatePreCoOrder(string customerId, OrderTypeEnum orderType, string partyCode);
        Task<BaseResponseViewModel<CoOrderResponse>> DeletePartyOrder(string customerId, PartyOrderType type, string partyCode, string memberId = null);
        Task<BaseResponseViewModel<dynamic>> RemovePartyMember(string customerId, string partyCode, string memberId = null);

    }
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IPaymentService _paymentService;
        private readonly IConfiguration _configuration;
        private readonly INotifyService _notifyService;
        private readonly IFirebaseMessagingService _fm;
        private readonly IBoxService _boxService;

        public OrderService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, IPaymentService paymentService, INotifyService notifyService, IFirebaseMessagingService fm, IBoxService boxService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;
            _paymentService = paymentService;
            _notifyService = notifyService;
            _fm = fm;
            _boxService = boxService;
        }

        #region User
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
                                    .FirstOrDefaultAsync(x => x.OrderId == Guid.Parse(id));

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

        public async Task<BaseResponseViewModel<CoOrderResponse>> GetPartyOrder(string customerId, string partyCode)
        {
            try
            {
                var keyCoOrder = RedisDbEnum.CoOrder.GetDisplayName() + ":" + partyCode;
                var partyOrder = await _unitOfWork.Repository<Party>().GetAll()
                                                .Where(x => x.PartyCode == partyCode)
                                                .ToListAsync();
                if (partyOrder == null)
                    throw new ErrorResponse(400, (int)PartyErrorEnums.INVALID_CODE, PartyErrorEnums.INVALID_CODE.GetDisplayName());
                else if (partyOrder.All(x => x.IsActive == false))
                    throw new ErrorResponse(404, (int)PartyErrorEnums.PARTY_DELETE, PartyErrorEnums.PARTY_DELETE.GetDisplayName());
                else if (partyOrder.FirstOrDefault(x => x.IsActive == true).Status == (int)PartyOrderStatus.OutOfTimeslot)
                    throw new ErrorResponse(404, (int)PartyErrorEnums.OUT_OF_TIMESLOT, PartyErrorEnums.OUT_OF_TIMESLOT.GetDisplayName());

                CoOrderResponse coOrder = null;
                if (partyOrder.FirstOrDefault().PartyType == (int)PartyOrderType.CoOrder)
                {
                    var redisValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyCoOrder, null);
                    coOrder = JsonConvert.DeserializeObject<CoOrderResponse>(redisValue);
                }

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

        public async Task<BaseResponseViewModel<CoOrderStatusResponse>> GetPartyStatus(string partyCode)
        {
            try
            {
                var keyCoOrder = RedisDbEnum.CoOrder.GetDisplayName() + ":" + partyCode;
                var result = new CoOrderStatusResponse();
                var party = await _unitOfWork.Repository<Party>().GetAll().FirstOrDefaultAsync(x => x.PartyCode == partyCode);

                var redisValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyCoOrder, null);
                CoOrderResponse coOrder = JsonConvert.DeserializeObject<CoOrderResponse>(redisValue);

                if (coOrder is not null)
                {
                    result = new CoOrderStatusResponse()
                    {
                        NumberOfMember = coOrder.PartyOrder.Where(x => x.Customer.IsAdmin == false).Count(),
                        IsReady = coOrder.PartyOrder.All(x => x.Customer.IsConfirm == true),
                        IsFinish = coOrder.IsPayment,
                        IsDelete = false,
                    };
                }
                else
                {
                    result.IsDelete = true;
                }
                return new BaseResponseViewModel<CoOrderStatusResponse>()
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
                    BoxQuantity = 1,
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
                        ImageUrl = productInMenu.Product.Product.ImageUrl,
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

        public async Task<BaseResponseViewModel<CreateReOrderResponse>> CreatePreOrderFromReOrder(string customerId, string reOrderId, OrderTypeEnum orderType)
        {
            try
            {
                var result = new CreateReOrderResponse()
                {
                    ProductCannotAdd = new List<string>()
                };
                var oldOrder = await _unitOfWork.Repository<Order>().GetAll().Where(x => x.Id == Guid.Parse(reOrderId)).FirstOrDefaultAsync();

                var listOrderDetailRequest = new List<CreatePreOrderDetailRequest>();
                foreach (var orderDetail in oldOrder.OrderDetails)
                {
                    var productInMenu = _unitOfWork.Repository<ProductInMenu>().GetAll()
                        .Include(x => x.Menu)
                        .Include(x => x.Product)
                        .Where(x => x.Id == orderDetail.ProductInMenuId && x.Menu.TimeSlotId == oldOrder.TimeSlotId)
                        .FirstOrDefault();

                    if (productInMenu != null && (productInMenu.IsActive != false || productInMenu.Status == (int)ProductInMenuStatusEnum.Avaliable))
                    {
                        var product = new CreatePreOrderDetailRequest()
                        {
                            ProductId = productInMenu.ProductId,
                            Quantity = orderDetail.Quantity,
                        };
                        listOrderDetailRequest.Add(product);
                    }
                    else
                    {
                        result.ProductCannotAdd.Add(productInMenu.Product.Name);
                    }
                }
                if (listOrderDetailRequest.Count() > 0)
                {
                    var request = new CreatePreOrderRequest()
                    {
                        OrderType = orderType,
                        TimeSlotId = oldOrder.TimeSlotId,
                        OrderDetails = listOrderDetailRequest
                    };

                    result.OrderResponse = CreatePreOrder(customerId, request).Result.Data;
                }
                return new BaseResponseViewModel<CreateReOrderResponse>()
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

        public async Task<BaseResponseViewModel<OrderResponse>> CreateOrder(string customerId, CreateOrderRequest request)
        {
            try
            {
                string partyCode;
                #region Timeslot
                var timeSlot = await _unitOfWork.Repository<TimeSlot>().FindAsync(x => x.Id == request.TimeSlotId);

                if (timeSlot.Id != Guid.Parse("E8D529D4-6A51-4FDB-B9DB-E29F54C0486E") && request.OrderType is OrderTypeEnum.OrderToday && !Utils.CheckTimeSlot(timeSlot))
                    throw new ErrorResponse(400, (int)TimeSlotErrorEnums.OUT_OF_TIMESLOT,
                        TimeSlotErrorEnums.OUT_OF_TIMESLOT.GetDisplayName());
                #endregion

                #region customer phone
                var customer = await _unitOfWork.Repository<Customer>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(customerId));

                if (customer.Phone is null)
                    throw new ErrorResponse(400, (int)CustomerErrorEnums.MISSING_PHONENUMBER,
                                            CustomerErrorEnums.MISSING_PHONENUMBER.GetDisplayName());
                #endregion

                #region station
                var station = await _unitOfWork.Repository<Station>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(request.StationId));

                if (station.IsAvailable == false)
                {
                    throw new ErrorResponse(400, (int)StationErrorEnums.UNAVAILABLE,
                                            StationErrorEnums.UNAVAILABLE.GetDisplayName());
                }
                #endregion

                var order = _mapper.Map<Order>(request);

                order.CustomerId = Guid.Parse(customerId);
                order.CheckInDate = DateTime.Now;
                order.OrderStatus = (int)OrderStatusEnum.PaymentPending;

                #region Party (if have)
                if (!request.PartyCode.IsNullOrEmpty())
                {
                    var checkCode = await _unitOfWork.Repository<Party>().GetAll()
                                    .Where(x => x.PartyCode == request.PartyCode)
                                    .ToListAsync();

                    if (checkCode == null)
                        throw new ErrorResponse(400, (int)PartyErrorEnums.INVALID_CODE, PartyErrorEnums.INVALID_CODE.GetDisplayName());

                    if (checkCode.Any(x => x.Status == (int)PartyOrderStatus.CloseParty))
                        throw new ErrorResponse(404, (int)PartyErrorEnums.PARTY_CLOSED, PartyErrorEnums.PARTY_CLOSED.GetDisplayName());

                    if (checkCode.FirstOrDefault().PartyType == (int)PartyOrderType.CoOrder)
                    {
                        var keyCoOrder = RedisDbEnum.CoOrder.GetDisplayName() + ":" + request.PartyCode;
                        var party = checkCode.FirstOrDefault(x => x.CustomerId == order.CustomerId);

                        var redisValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyCoOrder, null);
                        CoOrderResponse coOrder = JsonConvert.DeserializeObject<CoOrderResponse>(redisValue);

                        if (coOrder != null)
                        {
                            coOrder.IsPayment = true;
                            await ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, keyCoOrder, coOrder);
                        }
                        party.OrderId = order.Id;
                        party.UpdateAt = DateTime.Now;

                        await _unitOfWork.Repository<Party>().UpdateDetached(party);
                        partyCode = request.PartyCode;
                    }
                    else
                    {
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
                            checkJoin.UpdateAt = DateTime.Now;
                            await _unitOfWork.Repository<Party>().UpdateDetached(checkJoin);
                        }
                    }
                    order.IsPartyMode = true;
                }
                #endregion

                await _unitOfWork.Repository<Order>().InsertAsync(order);
                await _unitOfWork.CommitAsync();

                await _paymentService.CreatePayment(order, request.Point, request.PaymentType);

                order.OrderStatus = (int)OrderStatusEnum.Processing;

                await _unitOfWork.Repository<Order>().UpdateDetached(order);
                await _unitOfWork.CommitAsync();

                var resultOrder = _mapper.Map<OrderResponse>(order);
                resultOrder.Customer = _mapper.Map<CustomerOrderResponse>(customer);
                resultOrder.StationOrder = _unitOfWork.Repository<Station>().GetAll()
                                                .Where(x => x.Id == Guid.Parse(request.StationId))
                                                .ProjectTo<StationOrderResponse>(_mapper.ConfigurationProvider)
                                                .FirstOrDefault();

                #region Background Job noti
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
                #endregion

                #region split order + create order box
                SplitOrderAndCreateOrderBox(order);
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

        public async Task<BaseResponseViewModel<CoOrderResponse>> OpenParty(string customerId, CreatePreOrderRequest request)
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
                    TimeSlotId = (Guid)request.TimeSlotId,
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
                                TotalAmount = orderDetail.Quantity * productInMenu.Product.Price
                            };
                            orderCard.OrderDetails.Add(product);
                            orderCard.ItemQuantity += product.Quantity;
                            orderCard.TotalAmount += product.TotalAmount;
                        }
                    }
                    coOrder.PartyOrder.Add(orderCard);

                    var keyCoOrder = RedisDbEnum.CoOrder.GetDisplayName() + ":" + coOrder.PartyCode;
                    ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, keyCoOrder, coOrder);
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

        public async Task<BaseResponseViewModel<CoOrderResponse>> JoinPartyOrder(string customerId, string timeslotId, string partyCode)
        {
            try
            {
                var checkJoin = await _unitOfWork.Repository<Party>().GetAll()
                                                .FirstOrDefaultAsync(x => x.CustomerId == Guid.Parse(customerId)
                                                    && x.PartyCode != partyCode
                                                    && x.IsActive == true
                                                    && (x.Status == (int)PartyOrderStatus.NotConfirm
                                                    || x.Status == (int)PartyOrderStatus.NotRefund));

                if (checkJoin is not null && checkJoin.PartyType == (int)PartyOrderType.CoOrder)
                {
                    throw new ErrorResponse(400, (int)PartyErrorEnums.COORDER_PARTY_JOINED, PartyErrorEnums.COORDER_PARTY_JOINED.GetDisplayName() + $": {checkJoin.PartyCode}");
                }
                else if (checkJoin is not null && checkJoin.PartyType == (int)PartyOrderType.LinkedOrder)
                {
                    throw new ErrorResponse(400, (int)PartyErrorEnums.LINKED_PARTY_JOINED, PartyErrorEnums.LINKED_PARTY_JOINED.GetDisplayName() + $": {checkJoin.PartyCode}");
                }

                var listpartyOrder = await _unitOfWork.Repository<Party>().GetAll()
                                            .Where(x => x.PartyCode == partyCode)
                                            .ToListAsync();

                if (listpartyOrder.Any(x => x.CustomerId == Guid.Parse(customerId) && x.IsActive == true))
                {
                    var code = listpartyOrder.Find(x => x.CustomerId == Guid.Parse(customerId)).PartyCode;
                    throw new ErrorResponse(400, (int)PartyErrorEnums.PARTY_JOINED, PartyErrorEnums.PARTY_JOINED.GetDisplayName() + $": {code}");
                }
                else if (listpartyOrder.All(x => x.IsActive == false))
                {
                    throw new ErrorResponse(400, (int)PartyErrorEnums.PARTY_DELETE, PartyErrorEnums.PARTY_DELETE.GetDisplayName());
                }
                else if (listpartyOrder.Any(x => x.Status == (int)PartyOrderStatus.CloseParty))
                {
                    throw new ErrorResponse(400, (int)PartyErrorEnums.PARTY_CLOSED, PartyErrorEnums.PARTY_CLOSED.GetDisplayName());
                }
                else if (listpartyOrder == null)
                {
                    throw new ErrorResponse(400, (int)PartyErrorEnums.INVALID_CODE, PartyErrorEnums.INVALID_CODE.GetDisplayName());
                }
                else if (listpartyOrder.FirstOrDefault().PartyType == (int)PartyOrderType.LinkedOrder && listpartyOrder.FirstOrDefault().TimeSlotId != Guid.Parse(timeslotId))
                {
                    throw new ErrorResponse(400, (int)PartyErrorEnums.WRONG_TIMESLOT, PartyErrorEnums.WRONG_TIMESLOT.GetDisplayName() + $": {listpartyOrder.FirstOrDefault().TimeSlotId}");
                }

                var oldData = listpartyOrder.Find(x => x.CustomerId == Guid.Parse(customerId) && x.IsActive == false);

                switch (listpartyOrder.FirstOrDefault().PartyType)
                {
                    case (int)PartyOrderType.CoOrder:
                        var keyCoOrder = RedisDbEnum.CoOrder.GetDisplayName() + ":" + partyCode;
                        var redisValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyCoOrder, null);

                        CoOrderResponse coOrder = JsonConvert.DeserializeObject<CoOrderResponse>(redisValue);

                        if (listpartyOrder.FirstOrDefault().PartyType is (int)PartyOrderType.CoOrder)
                        {
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
                        }

                        if (oldData is null)
                        {
                            var newParty = new Party()
                            {
                                Id = Guid.NewGuid(),
                                CustomerId = Guid.Parse(customerId),
                                PartyCode = partyCode,
                                PartyType = (int)PartyOrderType.CoOrder,
                                Status = (int)PartyOrderStatus.NotConfirm,
                                IsActive = true,
                                CreateAt = DateTime.Now,
                            };
                            await _unitOfWork.Repository<Party>().InsertAsync(newParty);
                            await _unitOfWork.CommitAsync();
                        }
                        else
                        {
                            oldData.IsActive = true;
                            oldData.UpdateAt = DateTime.Now;

                            await _unitOfWork.Repository<Party>().UpdateDetached(oldData);
                            await _unitOfWork.CommitAsync();
                        }
                        ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, keyCoOrder, coOrder);
                        break;

                    case (int)PartyOrderType.LinkedOrder:
                        if (oldData is null)
                        {
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
                            await _unitOfWork.Repository<Party>().InsertAsync(newParty);
                            await _unitOfWork.CommitAsync();
                        }
                        else
                        {
                            oldData.IsActive = true;
                            oldData.UpdateAt = DateTime.Now;

                            await _unitOfWork.Repository<Party>().UpdateDetached(oldData);
                            await _unitOfWork.CommitAsync();
                        }
                        break;
                }
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

        public async Task<BaseResponseViewModel<AddProductToCardResponse>> AddProductToCard(string customerId, AddProductToCardRequest request)
        {
            try
            {
                var result = new AddProductToCardResponse();
                //check xem customer đã hết lượt đặt đơn hay chưa
                if (!request.TimeSlotId.Contains("E8D529D4-6A51-4FDB-B9DB-E29F54C0486E"))
                {
                    var customerOrder = await _unitOfWork.Repository<Order>().GetAll()
                                            .Where(x => x.CustomerId == Guid.Parse(customerId)
                                                && x.OrderStatus != (int)OrderStatusEnum.Finished
                                                && x.TimeSlotId == Guid.Parse(request.TimeSlotId))
                                            .ToListAsync();
                    if (customerOrder.Count() >= 2)
                    {
                        var customerToken = _unitOfWork.Repository<Fcmtoken>().GetAll().FirstOrDefault(x => x.UserId == Guid.Parse(customerId)).Token;

                        Notification notification = new Notification
                        {
                            Title = Constants.OUT_OF_LIMIT_ORDER,
                            Body = String.Format("Bạn chỉ 2 lượt đặt đơn trong cùng 1 khung giờ, vui lòng chờ các đơn bạn đã đặt được giao tới nhé!!!")
                        };

                        var data = new Dictionary<string, string>()
                    {
                        { "type", NotifyTypeEnum.ForPopup.ToString()}
                    };

                        BackgroundJob.Enqueue(() => _fm.SendToToken(customerToken, notification, data));

                        throw new ErrorResponse(400, (int)OrderErrorEnums.OUT_OF_LIMIT_ORDER,
                                                OrderErrorEnums.OUT_OF_LIMIT_ORDER.GetDisplayName());
                    }
                }
                var maxQuantity = Int32.Parse(_configuration["MaxQuantityInBox"]);

                if (request.Card.Select(x => x.Quantity).Sum() + request.Quantity >= maxQuantity)
                {
                    result.Status = new StatusViewModel()
                    {
                        Success = false,
                        Message = String.Format("Error"),
                        ErrorCode = 4002
                    };
                }
                else
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

                    result = new AddProductToCardResponse
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

                    var productWillAdd = new CheckFixBoxRequest()
                    {
                        Product = productRequest,
                        Quantity = request.Quantity
                    };
                    listProductInCard.Add(productWillAdd);

                    var addToBoxResult = ServiceHelpers.CheckProductFixTheBox(listProductInCard, productRequest, request.Quantity);

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
                }

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
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<CoOrderResponse>> AddProductIntoPartyCode(string customerId, string partyCode, CreatePreOrderRequest request)
        {
            try
            {
                var keyCoOrder = RedisDbEnum.CoOrder.GetDisplayName() + ":" + partyCode;
                var redisValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyCoOrder, null);
                CoOrderResponse coOrder = JsonConvert.DeserializeObject<CoOrderResponse>(redisValue);

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
                        TotalAmount = requestOD.Quantity * productInMenu.Product.Price
                    };

                    orderCard.OrderDetails.Add(product);
                    orderCard.ItemQuantity += product.Quantity;
                    orderCard.TotalAmount += product.TotalAmount;
                }

                ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, keyCoOrder, coOrder);

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
                partyOrder.UpdateAt = DateTime.Now;

                await _unitOfWork.Repository<Party>().UpdateDetached(partyOrder);
                await _unitOfWork.CommitAsync();

                var keyCoOrder = RedisDbEnum.CoOrder.GetDisplayName() + ":" + partyCode;
                var redisValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyCoOrder, null);
                CoOrderResponse coOrder = JsonConvert.DeserializeObject<CoOrderResponse>(redisValue);

                if (coOrder is null)
                    throw new ErrorResponse(400, (int)OrderErrorEnums.NOT_FOUND_COORDER, OrderErrorEnums.NOT_FOUND_COORDER.GetDisplayName());

                var orderCard = coOrder.PartyOrder.FirstOrDefault(x => x.Customer.Id == Guid.Parse(customerId));
                orderCard.Customer.IsConfirm = true;

                var rs = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, keyCoOrder, coOrder);

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
                var keyCoOrder = RedisDbEnum.CoOrder.GetDisplayName() + ":" + partyCode;
                var redisValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyCoOrder, null);
                CoOrderResponse coOrder = JsonConvert.DeserializeObject<CoOrderResponse>(redisValue);

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

                var numberMember = coOrder.PartyOrder.Count();
                List<CheckFixBoxRequest> listProductInCard = new List<CheckFixBoxRequest>();

                var listProductInCoOrder = coOrder.PartyOrder.SelectMany(x => x.OrderDetails);

                var numberbox = listProductInCoOrder.Count() / Int32.Parse(_configuration["MaxQuantityInBox"]);
                var boxQuantity = Math.Ceiling((double)numberbox);

                order.BoxQuantity = (int)boxQuantity;

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

        public async Task<BaseResponseViewModel<CoOrderResponse>> DeletePartyOrder(string customerId, PartyOrderType type, string partyCode, string memberId = null)
        {
            try
            {
                switch (type)
                {
                    case PartyOrderType.LinkedOrder:
                        var party = await _unitOfWork.Repository<Party>().GetAll()
                                                      .Where(x => x.PartyCode == partyCode && x.CustomerId == Guid.Parse(customerId))
                                                      .FirstOrDefaultAsync();
                        if (party is null) throw new ErrorResponse(400, (int)PartyErrorEnums.INVALID_CODE, PartyErrorEnums.INVALID_CODE.GetDisplayName());

                        party.IsActive = false;
                        party.UpdateAt = DateTime.Now;
                        await _unitOfWork.Repository<Party>().UpdateDetached(party);
                        await _unitOfWork.CommitAsync();
                        break;

                    case PartyOrderType.CoOrder:
                        var customerToken = "";
                        if (!memberId.IsNullOrEmpty()) { customerToken = _unitOfWork.Repository<Fcmtoken>().GetAll().FirstOrDefault(x => x.UserId == Guid.Parse(memberId)).Token; }

                        var keyCoOrder = RedisDbEnum.CoOrder.GetDisplayName() + ":" + partyCode;
                        var redisValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyCoOrder, null);
                        CoOrderResponse coOrder = JsonConvert.DeserializeObject<CoOrderResponse>(redisValue);

                        if (coOrder is null) throw new ErrorResponse(400, (int)OrderErrorEnums.NOT_FOUND_COORDER, OrderErrorEnums.NOT_FOUND_COORDER.GetDisplayName());
                        var orderOutMember = coOrder.PartyOrder.Find(x => x.Customer.Id == Guid.Parse(customerId));

                        var listParty = await _unitOfWork.Repository<Party>().GetAll()
                                                       .Where(x => x.PartyCode == partyCode)
                                                       .ToListAsync();
                        if (listParty is null) throw new ErrorResponse(400, (int)PartyErrorEnums.INVALID_CODE, PartyErrorEnums.INVALID_CODE.GetDisplayName());

                        var partyOutMember = listParty.FirstOrDefault(x => x.CustomerId == Guid.Parse(customerId));

                        partyOutMember.IsActive = false;
                        partyOutMember.UpdateAt = DateTime.Now;
                        await _unitOfWork.Repository<Party>().UpdateDetached(partyOutMember);
                        await _unitOfWork.CommitAsync();

                        // đứa delete là admin
                        if (orderOutMember.Customer.IsAdmin is true)
                        {
                            //nếu đơn nhóm chỉ có admin => xóa chế độ coOrder
                            if (memberId is null)
                            {
                                ServiceHelpers.GetSetDataRedis(RedisSetUpType.DELETE, keyCoOrder, null);
                            }
                            else
                            {
                                var partyOfTheChosenOne = coOrder.PartyOrder.FirstOrDefault(x => x.Customer.Id == Guid.Parse(memberId)).Customer;
                                partyOfTheChosenOne.IsAdmin = true;
                                partyOfTheChosenOne.IsConfirm = true;

                                coOrder.PartyOrder.Remove(orderOutMember);
                                ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, keyCoOrder, coOrder);

                                Notification notification = new Notification
                                {
                                    Title = Constants.CHANGE_ADMIN_PARTY,
                                    Body = String.Format($"Bạn vừa được {partyOfTheChosenOne.Name} chuyển quyền admin đơn nhóm {partyCode}")
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
                            coOrder.PartyOrder.Remove(orderOutMember);
                            ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, keyCoOrder, coOrder);
                        }
                        break;
                }

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

        public async Task<BaseResponseViewModel<dynamic>> RemovePartyMember(string customerId, string partyCode, string memberId = null)
        {
            try
            {
                var party = await _unitOfWork.Repository<Party>().GetAll().FirstOrDefaultAsync(x => x.CustomerId == Guid.Parse(memberId));
                party.IsActive = false;
                party.UpdateAt = DateTime.Now;

                await _unitOfWork.Repository<Party>().UpdateDetached(party);
                await _unitOfWork.CommitAsync();

                var keyCoOrder = RedisDbEnum.CoOrder.GetDisplayName() + ":" + partyCode;
                var redisValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyCoOrder, null);
                CoOrderResponse coOrder = JsonConvert.DeserializeObject<CoOrderResponse>(redisValue);

                var memParty = coOrder.PartyOrder.FirstOrDefault(x => x.Customer.Id == Guid.Parse(memberId));
                coOrder.PartyOrder.Remove(memParty);

                ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, keyCoOrder, coOrder);

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
        #endregion

        #region Staff
        public async Task<BaseResponsePagingViewModel<OrderForStaffResponse>> GetOrders(OrderForStaffResponse filter, PagingRequest paging)
        {
            try
            {
                var order = _unitOfWork.Repository<Order>().GetAll()
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

        public async void SplitOrderAndCreateOrderBox(Order order)
        {
            try
            {
                PackageOrderModel packageOrder = new PackageOrderModel()
                {
                    TotalConfirm = 0,
                    NumberHasConfirm = 0,
                    PackageOrderBoxes = new List<PackageOrderBoxModel>()
                };
                HashSet<KeyValuePair<Guid, string>> listBox = new HashSet<KeyValuePair<Guid, string>>();
                HashSet<KeyValuePair<Guid, string>> listStoreId = new HashSet<KeyValuePair<Guid, string>>();
                List<PackageOrderDetailModel> packageOrderDetails = new List<PackageOrderDetailModel>();
                PackageResponse packageResponse;

                //lấy boxId đã lock
                var keyOrder = RedisDbEnum.Box.GetDisplayName() + ":Order:" + order.OrderCode;
                var keyOrderPack = RedisDbEnum.OrderOperation.GetDisplayName() + ":" + order.OrderCode;

                List<Guid> listLockOrder = new List<Guid>();
                var redisLockValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyOrder, null);
                if (redisLockValue.HasValue == true)
                {
                    listLockOrder = JsonConvert.DeserializeObject<List<Guid>>(redisLockValue);
                }

                #region nhả lại số box đã lock
                var key = RedisDbEnum.Box.GetDisplayName() + ":Station";

                List<LockBoxinStationModel> listStationLockBox = new List<LockBoxinStationModel>();
                var redisStationValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, key, null);
                if (redisStationValue.HasValue == true)
                {
                    listStationLockBox = JsonConvert.DeserializeObject<List<LockBoxinStationModel>>(redisStationValue);
                }

                listStationLockBox = listStationLockBox.Select(x => new LockBoxinStationModel
                {
                    StationName = x.StationName,
                    StationId = x.StationId,
                    NumberBoxLockPending = x.NumberBoxLockPending - listLockOrder.Count(),
                }).ToList();

                ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, key, listStationLockBox);
                #endregion

                #region lưu đơn vào tủ
                foreach (var item in listLockOrder)
                {
                    var box = _unitOfWork.Repository<Box>().GetAll().FirstOrDefault(x => x.Id == item);

                    //add Orderbox
                    var orderBox = new OrderBox()
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        BoxId = item,
                        Status = (int)OrderBoxStatusEnum.NotPicked,
                        CreateAt = DateTime.Now
                    };
                    _unitOfWork.Repository<OrderBox>().InsertAsync(orderBox);
                    listBox.Add(new KeyValuePair<Guid, string>(box.Id, box.Code));
                    packageOrder.PackageOrderBoxes.Add(new PackageOrderBoxModel
                    {
                        BoxId = item,
                        BoxCode = box.Code,
                        PackageOrderDetailModels = new List<PackageOrderDetailModel>()
                    });
                }
                #endregion

                #region Ghi nhận cac product trong tủ
                if (order.IsPartyMode == true)
                {
                    var party = _unitOfWork.Repository<Party>().GetAll().FirstOrDefault(x => x.OrderId == order.Id);
                    if (party.PartyType == (int)PartyOrderType.CoOrder)
                    {
                        var keyCoOrder = RedisDbEnum.CoOrder.GetDisplayName() + ":" + party.PartyCode;

                        var redisValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyCoOrder, null);
                        CoOrderResponse coOrder = JsonConvert.DeserializeObject<CoOrderResponse>(redisValue);

                        foreach (var partyOrder in coOrder.PartyOrder)
                        {
                            for (int index = 0; index <= packageOrder.PackageOrderBoxes.Count(); index++)
                            {
                                var item = packageOrder.PackageOrderBoxes[index];
                                item.PackageOrderDetailModels = partyOrder.OrderDetails.Select(x => new PackageOrderDetailModel
                                {
                                    ProductId = x.ProductId,
                                    ProductName = x.ProductName,
                                    ProductInMenuId = x.ProductInMenuId,
                                    Quantity = x.Quantity
                                }).ToList();
                                break;
                            }
                        }
                    }
                }
                else
                {
                    var item = packageOrder.PackageOrderBoxes.FirstOrDefault();
                    foreach (var product in order.OrderDetails)
                    {
                        var productInMenu = _unitOfWork.Repository<ProductInMenu>().GetAll().FirstOrDefault(x => x.Id == product.ProductInMenuId);
                        item.PackageOrderDetailModels.Add(new PackageOrderDetailModel
                        {
                            ProductId = productInMenu.ProductId,
                            ProductName = productInMenu.Product.Name,
                            ProductInMenuId = productInMenu.Id,
                            Quantity = product.Quantity
                        });
                    }
                }
                #endregion

                //xử lý đơn 
                foreach (var od in order.OrderDetails)
                {
                    var store = _unitOfWork.Repository<Store>().GetAll().FirstOrDefault(x => x.Id == od.StoreId);
                    listStoreId.Add(new KeyValuePair<Guid, string>(store.Id, store.StoreName));
                }

                foreach (var storeId in listStoreId)
                {
                    var keyStaff = RedisDbEnum.Staff.GetDisplayName() + ":" + storeId.Value + ":" + order.TimeSlot.ArriveTime.ToString(@"hh\-mm\-ss");
                    var listOdByStore = order.OrderDetails.Where(x => x.StoreId == storeId.Key).ToList();
                    var redisValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyStaff, null);

                    if (redisValue.HasValue == false)
                    {
                        packageResponse = new PackageResponse()
                        {
                            TotalProductInDay = 0,
                            TotalProductPending = 0,
                            TotalProductError = 0,
                            TotalProductReady = 0,
                            ProductTotalDetails = new List<ProductTotalDetail>()
                        };
                    }
                    else
                    {
                        packageResponse = JsonConvert.DeserializeObject<PackageResponse>(redisValue);
                    }

                    // xử lý ProductTotalDetail
                    foreach (var orderDetail in listOdByStore)
                    {
                        packageOrder.TotalConfirm += 1;
                        var productInMenu = _unitOfWork.Repository<ProductInMenu>().GetAll().FirstOrDefault(x => x.Id == orderDetail.ProductInMenuId);
                        var productTotalDetail = packageResponse.ProductTotalDetails.Find(x => x.ProductInMenuId == orderDetail.ProductInMenuId);

                        packageOrderDetails.Add(new PackageOrderDetailModel()
                        {
                            ProductId = productInMenu.ProductId,
                            ProductName = productInMenu.Product.Name,
                            ProductInMenuId = orderDetail.ProductInMenuId,
                            Quantity = orderDetail.Quantity
                        });

                        if (productTotalDetail is null)
                        {
                            productTotalDetail = new ProductTotalDetail()
                            {
                                ProductId = productInMenu.ProductId,
                                ProductInMenuId = productInMenu.Id,
                                ProductName = productInMenu.Product.Name,
                                PendingQuantity = orderDetail.Quantity,
                                ReadyQuantity = 0,
                                ErrorQuantity = 0,
                                WaitingQuantity = 0,
                                ProductDetails = new List<ProductDetail>()
                            };
                            packageResponse.ProductTotalDetails.Add(productTotalDetail);
                        }
                        else
                        {
                            productTotalDetail.PendingQuantity += orderDetail.Quantity;
                        }
                        productTotalDetail = packageResponse.ProductTotalDetails.FirstOrDefault(x => x.ProductId == productInMenu.ProductId);

                        productTotalDetail.ProductDetails.Add(new ProductDetail()
                        {
                            OrderId = order.Id,
                            OrderCode = order.OrderCode,
                            StationId = (Guid)order.StationId,
                            CheckInDate = order.CheckInDate,
                            QuantityOfProduct = orderDetail.Quantity,
                            IsFinishPrepare = false,
                            IsAssignToShipper = false

                        });
                        packageResponse.TotalProductInDay += orderDetail.Quantity;
                        packageResponse.TotalProductPending += orderDetail.Quantity;

                        //xử lý PackageStationResponse
                        if (packageResponse.PackageStations is null || packageResponse.PackageStations.Find(x => x.StationId == order.StationId && x.IsShipperAssign == false) is null)
                        {
                            if (packageResponse.PackageStations is null)
                            {
                                packageResponse.PackageStations = new List<PackageStationResponse>();
                            }

                            var station = _unitOfWork.Repository<Station>().GetAll().FirstOrDefault(x => x.Id == order.StationId);
                            var stationPackage = new PackageStationResponse()
                            {
                                StationId = station.Id,
                                StationName = station.Name,
                                TotalQuantity = 0,
                                ReadyQuantity = 0,
                                IsShipperAssign = false,
                                PackageStationDetails = new List<PackageDetailResponse>(),
                                ListPackageMissing = new List<PackageDetailResponse>(),
                                ListOrder = new HashSet<KeyValuePair<Guid, string>>()
                            };
                            stationPackage.ListPackageMissing.Add(new PackageDetailResponse()
                            {
                                ProductId = productInMenu.ProductId,
                                ProductName = productInMenu.Product.Name,
                                Quantity = orderDetail.Quantity,
                            });

                            stationPackage.TotalQuantity += orderDetail.Quantity;
                            packageResponse.PackageStations.Add(stationPackage);
                        }
                        else
                        {
                            var stationPack = packageResponse.PackageStations.FirstOrDefault(x => x.StationId == order.StationId && x.IsShipperAssign == false);
                            var productMissing = stationPack.ListPackageMissing.FirstOrDefault(x => x.ProductId == productInMenu.ProductId);
                            if (productMissing is null)
                            {
                                stationPack.ListPackageMissing.Add(new PackageDetailResponse()
                                {
                                    ProductId = productInMenu.ProductId,
                                    ProductName = productInMenu.Product.Name,
                                    Quantity = orderDetail.Quantity,
                                });
                            }
                            else
                            {
                                productMissing.Quantity += orderDetail.Quantity;
                            }
                            stationPack.TotalQuantity += orderDetail.Quantity;
                        }
                    }
                    ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, keyStaff, packageResponse);
                }
                _unitOfWork.Commit();
                ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, keyOrderPack, packageOrder);
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public void UpdatePartyOrderStatus(string code)
        {
            var parties = _unitOfWork.Repository<Party>().GetAll()
                        .Where(x => x.PartyCode == code).ToList();
            if (parties != null)
            {
                foreach (var party in parties)
                {
                    party.Status = (int)PartyOrderStatus.OutOfTimeslot;
                    party.IsActive = false;
                    party.UpdateAt = DateTime.Now;
                    _unitOfWork.Repository<Party>().UpdateDetached(party);
                }
                _unitOfWork.Commit();
                var keyCoOrder = RedisDbEnum.CoOrder + ":" + code;
                ServiceHelpers.GetSetDataRedis(RedisSetUpType.DELETE, keyCoOrder, null);
            }
        }
        #endregion
    }
}