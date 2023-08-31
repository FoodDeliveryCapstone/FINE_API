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

namespace FINE.Service.Service
{
    public interface IOrderService
    {
        Task<BaseResponseViewModel<OrderResponse>> GetOrderById(string customerId, string orderId);

        Task<BaseResponsePagingViewModel<OrderForStaffResponse>> GetOrders(OrderForStaffResponse filter, PagingRequest paging);
        Task<BaseResponsePagingViewModel<OrderResponse>> GetOrderByCustomerId(string customerId, PagingRequest paging);
        Task<BaseResponseViewModel<dynamic>> GetOrderStatus(string orderId);
        Task<BaseResponseViewModel<CoOrderResponse>> GetPartyOrder(string partyCode);
        Task<BaseResponseViewModel<OrderResponse>> CreatePreOrder(string customerId, CreatePreOrderRequest request);
        Task<BaseResponseViewModel<OrderResponse>> CreateOrder(string customerId, CreateOrderRequest request);
        Task<BaseResponseViewModel<CoOrderResponse>> OpenCoOrder(string customerId, CreatePreOrderRequest request);
        Task<BaseResponseViewModel<CoOrderResponse>> JoinPartyOrder(string customerId, string partyCode);
        Task<BaseResponseViewModel<AddProductToCardResponse>> AddProductToCard(string? productId, double? volumeSpace, string timeSlotId);
        Task<BaseResponseViewModel<CoOrderResponse>> AddProductIntoPartyCode(string customerId, string partyCode, CreatePreOrderRequest request);
        Task<BaseResponseViewModel<CoOrderPartyCard>> FinalConfirmCoOrder(string customerId, string partyCode);
        Task<BaseResponseViewModel<OrderResponse>> CreatePreCoOrder(string customerId, OrderTypeEnum orderType, string partyCode);
        Task<BaseResponseViewModel<CoOrderResponse>> DeletePartyOrder(string customerId, string partyCode);
        Task<BaseResponseViewModel<OrderResponse>> UpdateOrderStatus(string orderId, UpdateOrderStatusRequest request);
        Task<BaseResponseViewModel<SimulateResponse>> SimulateOrder(SimulateRequest request);

    }
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IPaymentService _paymentService;
        private readonly IConfiguration _configuration;

        public OrderService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, IPaymentService paymentService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;
            _paymentService = paymentService;
        }

        public async Task<BaseResponsePagingViewModel<OrderResponse>> GetOrderByCustomerId(string customerId, PagingRequest paging)
        {
            try
            {
                var order = _unitOfWork.Repository<Data.Entity.Order>().GetAll()
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
                    OrderCode = DateTime.Now.ToString("ddMMyy_HHmm") + "-" + Utils.GenerateRandomCode(5) + "-" + customerId,
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
                        OrderId = order.Id,
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
                    OrderId = order.Id,
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
                                    .FirstOrDefaultAsync(x => x.PartyCode == request.PartyCode);

                    if (checkCode == null)
                        throw new ErrorResponse(400, (int)PartyErrorEnums.INVALID_CODE, PartyErrorEnums.INVALID_CODE.GetDisplayName());

                    if (checkCode.Status is (int)PartyOrderStatus.CloseParty)
                        throw new ErrorResponse(404, (int)PartyErrorEnums.PARTY_CLOSED, PartyErrorEnums.PARTY_CLOSED.GetDisplayName());

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

        public async Task<BaseResponseViewModel<CoOrderResponse>> GetPartyOrder(string partyCode)
        {
            try
            {
                var partyOrder = await _unitOfWork.Repository<Party>().GetAll()
                                                .Where(x => x.PartyCode == partyCode)
                                                .FirstOrDefaultAsync();
                if (partyOrder == null)
                    throw new ErrorResponse(400, (int)PartyErrorEnums.INVALID_CODE, PartyErrorEnums.INVALID_CODE.GetDisplayName());

                if (partyOrder.IsActive == false)
                    throw new ErrorResponse(404, (int)PartyErrorEnums.PARTY_DELETE, PartyErrorEnums.PARTY_DELETE.GetDisplayName());

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
                    PartyCode = Utils.GenerateRandomCode(6),
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
                    party.PartyType = (int)PartyOrderType.CoOrder;
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

        public async Task<BaseResponseViewModel<CoOrderResponse>> JoinPartyOrder(string customerId, string partyCode)
        {
            try
            {
                var listpartyOrder = await _unitOfWork.Repository<Party>().GetAll()
                                                .Where(x => x.PartyCode == partyCode && x.IsActive == true)
                                                .ToListAsync();

                var partyOrder = listpartyOrder.Find(x => x.Status != (int)PartyOrderStatus.CloseParty);

                if (listpartyOrder is null)
                    throw new ErrorResponse(400, (int)PartyErrorEnums.INVALID_CODE, PartyErrorEnums.INVALID_CODE.GetDisplayName());

                if (partyOrder is null)
                    throw new ErrorResponse(400, (int)PartyErrorEnums.PARTY_CLOSED, PartyErrorEnums.PARTY_CLOSED.GetDisplayName());

                if (listpartyOrder.Any(x => x.CustomerId == Guid.Parse(customerId)))
                    throw new ErrorResponse(400, (int)PartyErrorEnums.PARTY_JOINED, PartyErrorEnums.PARTY_JOINED.GetDisplayName());

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

                if (partyOrder.PartyType is (int)PartyOrderType.CoOrder)
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

        public async Task<BaseResponseViewModel<AddProductToCardResponse>> AddProductToCard(string? productId, double? volumeSpace ,string timeSlotId)
        {
            try
            {
                var result = new AddProductToCardResponse
                {
                    Product = new ProductInCard(),
                    ProductsRecommend = new List<ProductRecommend>()
                };

                if (productId is not null)
                {
                    var productRequest = _unitOfWork.Repository<ProductInMenu>().GetAll()
                                        .Include(x => x.Product)
                                        .Where(x => x.ProductId == Guid.Parse(productId)
                                            && x.Menu.TimeSlotId == Guid.Parse(timeSlotId))
                                        .GroupBy(x => x.Product)
                                        .Select(x => x.Key)
                                        .FirstOrDefault();

                    var addToBoxResult = ServiceHelpers.FillTheBox((double)volumeSpace, productRequest);
                    result.VolumeRemainingSpace = addToBoxResult.VolumeRemainingSpace;

                    result.Product = _mapper.Map<ProductInCard>(productRequest);

                    if (addToBoxResult.Success is true)
                    {
                        result.Product.Status = new StatusViewModel
                        {
                            Success = true,
                            Message = "Success",
                            ErrorCode = 200
                        };
                    }
                    else
                    {
                        result.Product.Status = new StatusViewModel
                        {
                            Success = false,
                            Message = OrderErrorEnums.CANNOT_ADD_TO_CARD.GetDisplayName(),
                            ErrorCode = (int)OrderErrorEnums.CANNOT_ADD_TO_CARD
                        };
                    }
                }
                else
                {
                    var boxSize = new
                    {
                        Height = Double.Parse(_configuration["BoxSize:Height"]),
                        Width = Double.Parse(_configuration["BoxSize:Width"]),
                        Length = Double.Parse(_configuration["BoxSize:Length"])
                    };
                    result.VolumeRemainingSpace = boxSize.Height * boxSize.Width * boxSize.Length;
                }

                result.ProductsRecommend = _unitOfWork.Repository<ProductInMenu>().GetAll()
                                                   .Include(x => x.Menu)
                                                   .Include(x => x.Product)
                                                   .Where(x => x.Menu.TimeSlotId == Guid.Parse(timeSlotId)
                                                      && (x.Product.Height * x.Product.Width * x.Product.Length) <= result.VolumeRemainingSpace)
                                                   .Select(x => x.Product)
                                                   .ProjectTo<ProductRecommend>(_mapper.ConfigurationProvider)
                                                   .ToList();

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
                                OrderId = order.Id,
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
                    OrderId = order.Id,
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

        public async Task<BaseResponseViewModel<CoOrderResponse>> DeletePartyOrder(string customerId, string partyCode)
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
                if (partyMem.Customer.IsAdmin is true)
                {
                    ServiceHelpers.GetSetDataRedis(RedisSetUpType.DELETE, partyCode);
                    foreach (var party in listParty)
                    {
                        party.IsActive = false;
                        await _unitOfWork.Repository<Party>().UpdateDetached(party);
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

        public async Task<BaseResponseViewModel<OrderResponse>> UpdateOrderStatus(string orderId, UpdateOrderStatusRequest request)
        {
            try
            {
                var order = await _unitOfWork.Repository<Data.Entity.Order>().GetAll()
                                    .FirstOrDefaultAsync(x => x.Id == Guid.Parse(orderId));
                if(order == null)
                {
                    throw new ErrorResponse(404, (int)OrderErrorEnums.NOT_FOUND,
                                       OrderErrorEnums.NOT_FOUND.GetDisplayName());
                }

                var updateOrderStatus = _mapper.Map<UpdateOrderStatusRequest, Data.Entity.Order>(request, order);

                updateOrderStatus.UpdateAt = DateTime.Now;

                await _unitOfWork.Repository<Data.Entity.Order>().UpdateDetached(updateOrderStatus);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<OrderResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0,
                    },
                    Data = _mapper.Map<OrderResponse>(updateOrderStatus)
                };
            }
            catch(ErrorResponse ex)
            {
                throw;
            }
        }

        public async Task<BaseResponseViewModel<SimulateResponse>> SimulateOrder(SimulateRequest request)
        {
            try
            {
                var rand = new Random();
                int errorCount = 0;
                var timeslot = await _unitOfWork.Repository<TimeSlot>().FindAsync(x => x.Id == Guid.Parse(request.TimeSlotId));
                var timeslotResponse = new TimeSlotOrderResponse()
                {
                    Id = timeslot.Id.ToString(),
                    CloseTime = timeslot.CloseTime,
                    ArriveTime = timeslot.ArriveTime,
                    CheckoutTime = timeslot.CheckoutTime,
                };

                SimulateResponse response = new SimulateResponse()
                {
                    Timeslot = timeslotResponse,
                    SingleOrderResult = new SimulateSingleOrderResponse()
                    {
                        OrderSuccess = new List<OrderSuccess>(),
                        OrderFailed = new List<OrderFailed>()
                    },
                    CoOrderOrderResult = new SimulateCoOrderResponse()
                    {
                        OrderSuccess = new List<OrderSuccess>(),
                        OrderFailed = new List<OrderFailed>()
                    }
                };

                var productInMenu = await _unitOfWork.Repository<ProductInMenu>().GetAll()
                            .Include(x => x.Menu)
                            .Include(x => x.Product)
                            .Where(x => x.Menu.TimeSlotId == Guid.Parse(request.TimeSlotId))
                            .GroupBy(x => x.Product)
                            .Select(x => x.Key)
                            .ToListAsync();
                var station = await _unitOfWork.Repository<Station>().GetAll().ToListAsync();
                var getAllCustomer = await _unitOfWork.Repository<Customer>().GetAll().ToListAsync();

                if (request.SingleOrder is not null)
                {                   
                    var listCustomer = getAllCustomer
                                            .OrderBy(x => rand.Next())
                                            .Take((int)request.SingleOrder.TotalCustomer)
                                            .ToList();
                   
                    foreach (var customer in listCustomer)
                    {
                        for (int quantity = 1; quantity <= request.SingleOrder.OrderPerPerson; quantity++)
                        {
                            var totalSingleOrderSuccess = request.SingleOrder.TotalOrderSuccess;
                            var totalSingleOrderFailed = request.SingleOrder.OrderPerPerson - request.SingleOrder.TotalOrderSuccess;
                            if (totalSingleOrderSuccess > 0 && totalSingleOrderFailed > 0) {
                                if(rand.Next() % 2 == 0) 
                                { 

                                    var payload = new CreatePreOrderRequest
                                    {
                                        OrderType = (int)OrderTypeEnum.OrderToday,
                                        TimeSlotId = Guid.Parse(request.TimeSlotId),
                                    };

                                    payload.OrderDetails = productInMenu
                                        .OrderBy(x => rand.Next())
                                        .Take(3)
                                        .Select(x => new CreatePreOrderDetailRequest
                                        {
                                            ProductId = x.Id,
                                            Quantity = 2
                                        })
                                        .ToList();
                                    try
                                    {
                                        var rs = CreatePreOrder(customer.Id.ToString(), payload).Result.Data;
                                        var stationId = station
                                                         .OrderBy(x => rand.Next())
                                                         .Select(x => x.Id)
                                                         .FirstOrDefault();

                                        var payloadCreateOrder = new CreateOrderRequest()
                                        {
                                            Id = rs.Id,
                                            OrderCode = rs.OrderCode,
                                            PartyCode = null,
                                            TotalAmount = rs.TotalAmount,
                                            FinalAmount = rs.FinalAmount,
                                            TotalOtherAmount = rs.TotalOtherAmount,
                                            OrderType = rs.OrderType,
                                            TimeSlotId = Guid.Parse(request.TimeSlotId),
                                            StationId = stationId.ToString(),
                                            PaymentType = 1,
                                            IsPartyMode = false,
                                            ItemQuantity = rs.ItemQuantity,
                                            Point = rs.Point,
                                            OrderDetails = rs.OrderDetails.Select(detail => new CreateOrderDetail()
                                            {
                                                Id = detail.Id,
                                                OrderId = detail.Id,
                                                ProductInMenuId = detail.ProductInMenuId,
                                                StoreId = detail.StoreId,
                                                ProductCode = detail.ProductCode,
                                                ProductName = detail.ProductName,
                                                UnitPrice = detail.UnitPrice,
                                                Quantity = detail.Quantity,
                                                TotalAmount = detail.TotalAmount,
                                                FinalAmount = detail.FinalAmount,
                                                Note = detail.Note

                                            }).ToList(),
                                            OtherAmounts = rs.OtherAmounts
                                        };

                                        try
                                        {
                                            var result = CreateOrder(customer.Id.ToString(), payloadCreateOrder).Result.Data;

                                            var orderSuccess = new OrderSuccess
                                            {
                                                Id = result.Id,
                                                OrderCode = result.OrderCode,
                                                Customer = result.Customer,
                                            };
                                            response.SingleOrderResult.OrderSuccess.Add(orderSuccess);
                                        }
                                        catch (Exception ex)
                                        {
                                            ErrorResponse err = (ErrorResponse)ex.InnerException;
                                            var orderFail = new OrderFailed
                                            {
                                                Status = new StatusViewModel
                                                {
                                                    Message = err.Error.Message,
                                                    Success = false,
                                                    ErrorCode = err.Error.ErrorCode,
                                                }
                                            };
                                            response.SingleOrderResult.OrderFailed.Add(orderFail);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        ErrorResponse err = (ErrorResponse)ex.InnerException;
                                        var orderFail = new OrderFailed
                                        {
                                            Status = new StatusViewModel
                                            {
                                                Message = err.Error.Message,
                                                Success = false,
                                                ErrorCode = err.Error.ErrorCode,
                                            }
                                        };
                                        response.SingleOrderResult.OrderFailed.Add(orderFail);
                                    }
                                    totalSingleOrderSuccess--;
                                }
                                else
                                {
                                    int randomError = rand.Next(1, 3);
                                    if(randomError == (int)SimulateOrderFailedType.Payment)
                                    {
                                        var orderFail = new OrderFailed
                                        {
                                            Status = new StatusViewModel
                                            {
                                                Message = "Balance is not enough",
                                                Success = false,
                                                ErrorCode = 400,
                                            }
                                        };
                                        response.SingleOrderResult.OrderFailed.Add(orderFail);
                                    }
                                    else if (randomError == (int)SimulateOrderFailedType.OutTimeslot)
                                    {
                                        var orderFail = new OrderFailed
                                        {
                                            Status = new StatusViewModel
                                            {
                                                Message = "Out of Time Slot!",
                                                Success = false,
                                                ErrorCode = 400,
                                            }
                                        };
                                        response.SingleOrderResult.OrderFailed.Add(orderFail);
                                    }
                                    else if (randomError == (int)SimulateOrderFailedType.OutOfProduct)
                                    {
                                        var orderFail = new OrderFailed
                                        {
                                            Status = new StatusViewModel
                                            {
                                                Message = "This product is not avaliable!",
                                                Success = false,
                                                ErrorCode = 400,
                                            }
                                        };
                                        response.SingleOrderResult.OrderFailed.Add(orderFail);
                                    }
                                    totalSingleOrderFailed--;
                                }
                            }
                            else if(totalSingleOrderSuccess > 0)
                            {
                                var payload = new CreatePreOrderRequest
                                {
                                    OrderType = (int)OrderTypeEnum.OrderToday,
                                    TimeSlotId = Guid.Parse(request.TimeSlotId),
                                };


                                payload.OrderDetails = productInMenu
                                    .OrderBy(x => rand.Next())
                                    .Take(3)
                                    .Select(x => new CreatePreOrderDetailRequest
                                    {
                                        ProductId = x.Id,
                                        Quantity = 2
                                    })
                                    .ToList();
                                try
                                {
                                    var rs = CreatePreOrder(customer.Id.ToString(), payload).Result.Data;

                                    var stationId = station
                                                     .OrderBy(x => rand.Next())
                                                     .Select(x => x.Id)
                                                     .FirstOrDefault();

                                    var payloadCreateOrder = new CreateOrderRequest()
                                    {
                                        Id = rs.Id,
                                        OrderCode = rs.OrderCode,
                                        PartyCode = null,
                                        TotalAmount = rs.TotalAmount,
                                        FinalAmount = rs.FinalAmount,
                                        TotalOtherAmount = rs.TotalOtherAmount,
                                        OrderType = rs.OrderType,
                                        TimeSlotId = Guid.Parse(request.TimeSlotId),
                                        StationId = stationId.ToString(),
                                        PaymentType = 1,
                                        IsPartyMode = false,
                                        ItemQuantity = rs.ItemQuantity,
                                        Point = rs.Point,
                                        OrderDetails = rs.OrderDetails.Select(detail => new CreateOrderDetail()
                                        {
                                            Id = detail.Id,
                                            OrderId = detail.Id,
                                            ProductInMenuId = detail.ProductInMenuId,
                                            StoreId = detail.StoreId,
                                            ProductCode = detail.ProductCode,
                                            ProductName = detail.ProductName,
                                            UnitPrice = detail.UnitPrice,
                                            Quantity = detail.Quantity,
                                            TotalAmount = detail.TotalAmount,
                                            FinalAmount = detail.FinalAmount,
                                            Note = detail.Note

                                        }).ToList(),
                                        OtherAmounts = rs.OtherAmounts
                                    };

                                    try
                                    {
                                        var result = CreateOrder(customer.Id.ToString(), payloadCreateOrder).Result.Data;
                                        var orderSuccess = new OrderSuccess
                                        {
                                            Id = result.Id,
                                            OrderCode = result.OrderCode,
                                            Customer = result.Customer,
                                        };
                                        response.SingleOrderResult.OrderSuccess.Add(orderSuccess);
                                    }
                                    catch (Exception ex)
                                    {
                                        errorCount++;
                                        ErrorResponse err = (ErrorResponse)ex.InnerException;
                                        var orderFail = new OrderFailed
                                        {
                                            Status = new StatusViewModel
                                            {
                                                Message = err.Error.Message,
                                                Success = false,
                                                ErrorCode = err.Error.ErrorCode,
                                            }
                                        };
                                        response.SingleOrderResult.OrderFailed.Add(orderFail);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    errorCount++;
                                    ErrorResponse err = (ErrorResponse)ex.InnerException;
                                    var orderFail = new OrderFailed
                                    {
                                        Status = new StatusViewModel
                                        {
                                            Message = err.Error.Message,
                                            Success = false,
                                            ErrorCode = err.Error.ErrorCode,
                                        }
                                    };
                                    response.SingleOrderResult.OrderFailed.Add(orderFail);
                                }
                                totalSingleOrderSuccess--;
                            }
                            else if(totalSingleOrderFailed > 0)
                            {
                                int randomError = rand.Next(1, 3);
                                if (randomError == (int)SimulateOrderFailedType.Payment)
                                {
                                    var orderFail = new OrderFailed
                                    {
                                        Status = new StatusViewModel
                                        {
                                            Message = "Balance is not enough",
                                            Success = false,
                                            ErrorCode = 400,
                                        }
                                    };
                                    response.SingleOrderResult.OrderFailed.Add(orderFail);
                                }
                                else if (randomError == (int)SimulateOrderFailedType.OutTimeslot)
                                {
                                    var orderFail = new OrderFailed
                                    {
                                        Status = new StatusViewModel
                                        {
                                            Message = "Out of Time Slot!",
                                            Success = false,
                                            ErrorCode = 400,
                                        }
                                    };
                                    response.SingleOrderResult.OrderFailed.Add(orderFail);
                                }
                                else if (randomError == (int)SimulateOrderFailedType.OutOfProduct)
                                {
                                    var orderFail = new OrderFailed
                                    {
                                        Status = new StatusViewModel
                                        {
                                            Message = "This product is not avaliable!",
                                            Success = false,
                                            ErrorCode = 400,
                                        }
                                    };
                                    response.SingleOrderResult.OrderFailed.Add(orderFail);
                                }
                                totalSingleOrderFailed--;

                            }
                        }
                    }
                }
                if (request.CoOrderOrder is not null)
                {
                    var listCustomer = getAllCustomer
                                           .OrderBy(x => rand.Next())
                                           .Take((int)request.CoOrderOrder.CustomerEach)
                                           .ToList();
                    var openCoOrderCustomer = listCustomer.OrderBy(x => rand.Next()).Take(1).FirstOrDefault();
                    var restCustomers = listCustomer.Skip(1).ToList();
                    
                    var totalCoOrderSuccess = request.CoOrderOrder.TotalOrderSuccess;
                    var totalCoOrderFailed = request.CoOrderOrder.TotalOrder - request.CoOrderOrder.TotalOrderSuccess;

                    for (int quantity = 1; quantity <= request.CoOrderOrder.TotalOrder; quantity++)
                    {
                        if (totalCoOrderSuccess > 0 && totalCoOrderFailed > 0)
                        {
                            if (rand.Next() % 2 == 0)
                            {
                                var payloadPreOrder = new CreatePreOrderRequest
                                {
                                    OrderType = (int)OrderTypeEnum.OrderToday,
                                    TimeSlotId = Guid.Parse(request.TimeSlotId),
                                    PartyType = (int)PartyOrderType.CoOrder
                                };

                                payloadPreOrder.OrderDetails = productInMenu
                                    .OrderBy(x => rand.Next())
                                    .Take(2)
                                    .Select(x => new CreatePreOrderDetailRequest
                                    {
                                        ProductId = x.Id,
                                        Quantity = 1
                                    })
                                    .ToList();

                                var openCoOrder = OpenCoOrder(openCoOrderCustomer.Id.ToString(), payloadPreOrder);

                                try 
                                { 
                                    foreach (var cus in restCustomers)
                                    {
                                        var joinCoOrder = JoinPartyOrder(cus.Id.ToString(), openCoOrder.Result.Data.PartyCode);
                                        var cusPayloadPreOrder = new CreatePreOrderRequest
                                        {
                                            OrderType = (int)OrderTypeEnum.OrderToday,
                                            TimeSlotId = Guid.Parse(request.TimeSlotId),
                                            PartyType = (int)PartyOrderType.CoOrder
                                        };

                                        cusPayloadPreOrder.OrderDetails = productInMenu
                                            .OrderBy(x => rand.Next())
                                            .Take(1)
                                            .Select(x => new CreatePreOrderDetailRequest
                                            {
                                                ProductId = x.Id,
                                                Quantity = 1
                                            })
                                            .ToList();
                                        var addProduct = AddProductIntoPartyCode(cus.Id.ToString(), openCoOrder.Result.Data.PartyCode, cusPayloadPreOrder);
                                        var confirmCoOrder = FinalConfirmCoOrder(cus.Id.ToString(), openCoOrder.Result.Data.PartyCode);
                                    }
                                
                                
                                    var preCoOrder = CreatePreCoOrder(openCoOrderCustomer.Id.ToString(), (int)payloadPreOrder.OrderType, openCoOrder.Result.Data.PartyCode).Result.Data;                                  
               
                                    var stationId = station
                                                        .OrderBy(x => rand.Next())
                                                        .Select(x => x.Id)
                                                        .FirstOrDefault();

                                    var payloadCreateOrder = new CreateOrderRequest()
                                    {
                                        Id = preCoOrder.Id,
                                        OrderCode = preCoOrder.OrderCode,
                                        PartyCode = openCoOrder.Result.Data.PartyCode,
                                        TotalAmount = preCoOrder.TotalAmount,
                                        FinalAmount = preCoOrder.FinalAmount,
                                        TotalOtherAmount = preCoOrder.TotalOtherAmount,
                                        OrderType = preCoOrder.OrderType,
                                        TimeSlotId = Guid.Parse(request.TimeSlotId),
                                        StationId = stationId.ToString(),
                                        PaymentType = 1,
                                        IsPartyMode = true,
                                        ItemQuantity = preCoOrder.ItemQuantity,
                                        Point = preCoOrder.Point,
                                        OrderDetails = preCoOrder.OrderDetails.Select(detail => new CreateOrderDetail()
                                        {
                                            Id = detail.Id,
                                            OrderId = detail.Id,
                                            ProductInMenuId = detail.ProductInMenuId,
                                            StoreId = detail.StoreId,
                                            ProductCode = detail.ProductCode,
                                            ProductName = detail.ProductName,
                                            UnitPrice = detail.UnitPrice,
                                            Quantity = detail.Quantity,
                                            TotalAmount = detail.TotalAmount,
                                            FinalAmount = detail.FinalAmount,
                                            Note = detail.Note

                                        }).ToList(),
                                        OtherAmounts = preCoOrder.OtherAmounts
                                    };
                                    try
                                    {
                                        var result = CreateOrder(openCoOrderCustomer.Id.ToString(), payloadCreateOrder).Result.Data;

                                        var orderSuccess = new OrderSuccess
                                        {
                                            Id = result.Id,
                                            OrderCode = result.OrderCode,
                                            Customer = result.Customer,
                                        };
                                        response.CoOrderOrderResult.OrderSuccess.Add(orderSuccess);
                                    }
                                    catch (Exception ex)
                                    {
                                        ErrorResponse err = (ErrorResponse)ex.InnerException;
                                        var orderFail = new OrderFailed
                                        {
                                            Status = new StatusViewModel
                                            {
                                                Message = err.Error.Message,
                                                Success = false,
                                                ErrorCode = err.Error.ErrorCode,
                                            }
                                        };
                                        response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (ex is ErrorResponse errorResponse)
                                    {
                                        ErrorResponse err = (ErrorResponse)ex.InnerException;
                                        var orderFail = new OrderFailed
                                        {
                                            Status = new StatusViewModel
                                            {
                                                Message = err.Error.Message,
                                                Success = false,
                                                ErrorCode = err.Error.ErrorCode,
                                            }
                                        };
                                        response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                                    }
                                    else
                                    {
                                        var orderFail = new OrderFailed
                                        {
                                            Status = new StatusViewModel
                                            {
                                                Message = ex.Message,
                                                Success = false,
                                                ErrorCode = 400
                                            }
                                        };
                                        response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                                    }
                                }
                                totalCoOrderSuccess--;
                            }
                            else
                            {
                                int randomError = rand.Next(1, 3);
                                if (randomError == (int)SimulateOrderFailedType.Payment)
                                {
                                    var orderFail = new OrderFailed
                                    {
                                        Status = new StatusViewModel
                                        {
                                            Message = "Balance is not enough",
                                            Success = false,
                                            ErrorCode = 400,
                                        }
                                    };
                                    response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                                }
                                else if (randomError == (int)SimulateOrderFailedType.OutTimeslot)
                                {
                                    var orderFail = new OrderFailed
                                    {
                                        Status = new StatusViewModel
                                        {
                                            Message = "Out of Time Slot!",
                                            Success = false,
                                            ErrorCode = 400,
                                        }
                                    };
                                    response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                                }
                                else if (randomError == (int)SimulateOrderFailedType.OutOfProduct)
                                {
                                    var orderFail = new OrderFailed
                                    {
                                        Status = new StatusViewModel
                                        {
                                            Message = "This product is not avaliable!",
                                            Success = false,
                                            ErrorCode = 400,
                                        }
                                    };
                                    response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                                }
                                totalCoOrderFailed--;
                            }                        
                        }
                        else if(totalCoOrderSuccess>0)
                        {

                            var payloadPreOrder = new CreatePreOrderRequest
                            {
                                OrderType = (int)OrderTypeEnum.OrderToday,
                                TimeSlotId = Guid.Parse(request.TimeSlotId),
                                PartyType = (int)PartyOrderType.CoOrder
                            };

                            payloadPreOrder.OrderDetails = productInMenu
                                .OrderBy(x => rand.Next())
                                .Take(2)
                                .Select(x => new CreatePreOrderDetailRequest
                                {
                                    ProductId = x.Id,
                                    Quantity = 1
                                })
                                .ToList();

                            var openCoOrder = OpenCoOrder(openCoOrderCustomer.Id.ToString(), payloadPreOrder);

                            try 
                            { 
                                foreach (var cus in restCustomers)
                                {
                                    var joinCoOrder = JoinPartyOrder(cus.Id.ToString(), openCoOrder.Result.Data.PartyCode);
                                    var cusPayloadPreOrder = new CreatePreOrderRequest
                                    {
                                        OrderType = (int)OrderTypeEnum.OrderToday,
                                        TimeSlotId = Guid.Parse(request.TimeSlotId),
                                        PartyType = (int)PartyOrderType.CoOrder
                                    };

                                    cusPayloadPreOrder.OrderDetails = productInMenu
                                        .OrderBy(x => rand.Next())
                                        .Take(1)
                                        .Select(x => new CreatePreOrderDetailRequest
                                        {
                                            ProductId = x.Id,
                                            Quantity = 1
                                        })
                                        .ToList();
                                    var addProduct = AddProductIntoPartyCode(cus.Id.ToString(), openCoOrder.Result.Data.PartyCode, cusPayloadPreOrder);
                                    var confirmCoOrder = FinalConfirmCoOrder(cus.Id.ToString(), openCoOrder.Result.Data.PartyCode);
                                }
                            
                            
                                var preCoOrder = CreatePreCoOrder(openCoOrderCustomer.Id.ToString(), (int)payloadPreOrder.OrderType, openCoOrder.Result.Data.PartyCode).Result.Data;

                                var stationId = station
                                                    .OrderBy(x => rand.Next())
                                                    .Select(x => x.Id)
                                                    .FirstOrDefault();

                                var payloadCreateOrder = new CreateOrderRequest()
                                {
                                    Id = preCoOrder.Id,
                                    OrderCode = preCoOrder.OrderCode,
                                    PartyCode = openCoOrder.Result.Data.PartyCode,
                                    TotalAmount = preCoOrder.TotalAmount,
                                    FinalAmount = preCoOrder.FinalAmount,
                                    TotalOtherAmount = preCoOrder.TotalOtherAmount,
                                    OrderType = preCoOrder.OrderType,
                                    TimeSlotId = Guid.Parse(request.TimeSlotId),
                                    StationId = stationId.ToString(),
                                    PaymentType = 1,
                                    IsPartyMode = true,
                                    ItemQuantity = preCoOrder.ItemQuantity,
                                    Point = preCoOrder.Point,
                                    OrderDetails = preCoOrder.OrderDetails.Select(detail => new CreateOrderDetail()
                                    {
                                        Id = detail.Id,
                                        OrderId = detail.Id,
                                        ProductInMenuId = detail.ProductInMenuId,
                                        StoreId = detail.StoreId,
                                        ProductCode = detail.ProductCode,
                                        ProductName = detail.ProductName,
                                        UnitPrice = detail.UnitPrice,
                                        Quantity = detail.Quantity,
                                        TotalAmount = detail.TotalAmount,
                                        FinalAmount = detail.FinalAmount,
                                        Note = detail.Note

                                    }).ToList(),
                                    OtherAmounts = preCoOrder.OtherAmounts
                                };
                                try
                                {
                                    var result = CreateOrder(openCoOrderCustomer.Id.ToString(), payloadCreateOrder).Result.Data;                                   

                                    var orderSuccess = new OrderSuccess
                                    {
                                        Id = result.Id,
                                        OrderCode = result.OrderCode,
                                        Customer = result.Customer,
                                    };
                                    response.CoOrderOrderResult.OrderSuccess.Add(orderSuccess);
                                }
                                catch (Exception ex)
                                {
                                    ErrorResponse err = (ErrorResponse)ex.InnerException;
                                    var orderFail = new OrderFailed
                                    {
                                        Status = new StatusViewModel
                                        {
                                            Message = err.Error.Message,
                                            Success = false,
                                            ErrorCode = err.Error.ErrorCode,
                                        }
                                    };
                                    response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                                }
                            }
                            catch (Exception ex)
                            {
                                if (ex is ErrorResponse errorResponse)
                                {
                                    ErrorResponse err = (ErrorResponse)ex.InnerException;
                                    var orderFail = new OrderFailed
                                    {
                                        Status = new StatusViewModel
                                        {
                                            Message = err.Error.Message,
                                            Success = false,
                                            ErrorCode = err.Error.ErrorCode,
                                        }
                                    };
                                    response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                                }
                                else 
                                {
                                    var orderFail = new OrderFailed
                                    {
                                        Status = new StatusViewModel
                                        {
                                            Message = ex.Message,
                                            Success = false,
                                            ErrorCode = 400 
                                        }
                                    };
                                    response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                                }
                            }
                            totalCoOrderSuccess--;
                        }
                        else if (totalCoOrderFailed > 0)
                        {
                            int randomError = rand.Next(1, 3);
                            if (randomError == (int)SimulateOrderFailedType.Payment)
                            {
                                var orderFail = new OrderFailed
                                {
                                    Status = new StatusViewModel
                                    {
                                        Message = "Balance is not enough",
                                        Success = false,
                                        ErrorCode = 400,
                                    }
                                };
                                response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                            }
                            else if (randomError == (int)SimulateOrderFailedType.OutTimeslot)
                            {
                                var orderFail = new OrderFailed
                                {
                                    Status = new StatusViewModel
                                    {
                                        Message = "Out of Time Slot!",
                                        Success = false,
                                        ErrorCode = 400,
                                    }
                                };
                                response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                            }
                            else if (randomError == (int)SimulateOrderFailedType.OutOfProduct)
                            {
                                var orderFail = new OrderFailed
                                {
                                    Status = new StatusViewModel
                                    {
                                        Message = "This product is not avaliable!",
                                        Success = false,
                                        ErrorCode = 400,
                                    }
                                };
                                response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                            }
                            totalCoOrderFailed--;
                        }
                    }
                }
                    
                return new BaseResponseViewModel<SimulateResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0,
                    },
                    Data = response
                };

            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}