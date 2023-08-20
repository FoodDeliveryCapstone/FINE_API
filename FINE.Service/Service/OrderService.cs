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
using StackExchange.Redis;
using Newtonsoft.Json;
using ServiceStack.Redis;
using Azure.Core;
using ServiceStack.Web;
using Microsoft.AspNetCore.Mvc;
using FINE.Service.Helpers;

namespace FINE.Service.Service
{
    public interface IOrderService
    {
        Task<BaseResponseViewModel<OrderResponse>> GetOrderById(string customerId, string id);
        Task<BaseResponsePagingViewModel<OrderForStaffResponse>> GetOrders(OrderForStaffResponse filter, PagingRequest paging);
        Task<BaseResponsePagingViewModel<OrderResponse>> GetOrderByCustomerId(string id, PagingRequest paging);
        Task<BaseResponseViewModel<CoOrderResponse>> GetPartyOrder(string partyCode);
        Task<BaseResponseViewModel<OrderResponse>> CreatePreOrder(string customerId, CreatePreOrderRequest request);
        Task<BaseResponseViewModel<OrderResponse>> CreateOrder(string id, CreateOrderRequest request);
        Task<BaseResponseViewModel<CoOrderResponse>> OpenCoOrder(string customerId, CreatePreOrderRequest request);
        Task<BaseResponseViewModel<CoOrderResponse>> JoinPartyOrder(string customerId, string partyCode);
        Task<BaseResponseViewModel<CoOrderResponse>> AddProductIntoPartyCode(string customerId, string partyCode, CreatePreOrderRequest request);
        Task<BaseResponseViewModel<CoOrderPartyCard>> FinalConfirmCoOrder(string customerId, string partyCode);
        Task<BaseResponseViewModel<OrderResponse>> CreatePreCoOrder(string customerId, string timeSlotId, string partyCode);
        Task<BaseResponseViewModel<CoOrderResponse>> DeletePartyOrder(string customerId, string partyCode);

        //Task<BaseResponseViewModel<OrderResponse>> ConfirmCoOrder(string customerId, CreatePreOrderRequest request);
        //Task<BaseResponseViewModel<dynamic>> UpdateOrder(string id, UpdateOrderTypeEnum orderStatus, UpdateOrderRequest request);
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

        public async Task<BaseResponseViewModel<OrderResponse>> CreatePreOrder(string customerId, CreatePreOrderRequest request)
        {
            try
            {
                #region check timeslot
                var timeSlot = _unitOfWork.Repository<TimeSlot>().Find(x => x.Id == request.TimeSlotId);

                if (timeSlot == null || timeSlot.IsActive == false)
                    throw new ErrorResponse(404, (int)TimeSlotErrorEnums.TIMESLOT_UNAVAILIABLE,
                        TimeSlotErrorEnums.TIMESLOT_UNAVAILIABLE.GetDisplayName());

                if (request.OrderType == (int)OrderTypeEnum.OrderToday && !Utils.CheckTimeSlot(timeSlot))
                    throw new ErrorResponse(400, (int)TimeSlotErrorEnums.OUT_OF_TIMESLOT,
                        TimeSlotErrorEnums.OUT_OF_TIMESLOT.GetDisplayName());
                #endregion

                var order = new OrderResponse()
                {
                    Id = Guid.NewGuid(),
                    OrderCode = DateTime.Now.ToString("ddMMyy_HHmm") + "-" + Utils.GenerateRandomCode() + "-" + customerId,
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
                var timeSlot = await _unitOfWork.Repository<TimeSlot>().FindAsync(x => x.Id == request.TimeSlotId);

                if (request.OrderType is (int)OrderTypeEnum.OrderToday && !Utils.CheckTimeSlot(timeSlot))
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

                if (request.OrderCode is not null)
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

                if (!Utils.CheckTimeSlot(timeSlot))
                    throw new ErrorResponse(400, (int)TimeSlotErrorEnums.OUT_OF_TIMESLOT,
                        TimeSlotErrorEnums.OUT_OF_TIMESLOT.GetDisplayName());
                #endregion

                var customer = await _unitOfWork.Repository<Customer>().GetAll()
                                    .FirstOrDefaultAsync(x => x.Id == Guid.Parse(customerId));

                var coOrder = new CoOrderResponse()
                {
                    Id = Guid.NewGuid(),
                    PartyCode = Utils.GenerateRandomCode(),
                    TimeSlot = _mapper.Map<TimeSlotOrderResponse>(timeSlot),
                    PartyOrder = new List<CoOrderPartyCard>()
                };

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

                var party = new Party()
                {
                    Id = Guid.NewGuid(),
                    CustomerId = Guid.Parse(customerId),
                    PartyCode = coOrder.PartyCode,
                    Status = (int)PartyOrderStatus.NotConfirm,
                    IsActive = true,
                    CreateAt = DateTime.Now
                };
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
                var partyOrder = await _unitOfWork.Repository<Party>().GetAll()
                                                .Where(x => x.PartyCode == partyCode)
                                                .FirstOrDefaultAsync();
                if (partyOrder == null)
                    throw new ErrorResponse(400, (int)PartyErrorEnums.INVALID_CODE, PartyErrorEnums.INVALID_CODE.GetDisplayName());

                if (partyOrder.Status is (int)PartyOrderStatus.CloseParty)
                    throw new ErrorResponse(404, (int)PartyErrorEnums.PARTY_CLOSED, PartyErrorEnums.PARTY_CLOSED.GetDisplayName());

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

                if (!Utils.CheckTimeSlot(timeSlot))
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

        public async Task<BaseResponseViewModel<OrderResponse>> CreatePreCoOrder(string customerId, string timeSlotId, string partyCode)
        {
            try
            {
                CoOrderResponse coOrder = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, partyCode);

                if (coOrder is null)
                    throw new ErrorResponse(400, (int)OrderErrorEnums.NOT_FOUND_COORDER, OrderErrorEnums.NOT_FOUND_COORDER.GetDisplayName());

                #region check timeslot
                var timeSlot = _unitOfWork.Repository<TimeSlot>().Find(x => x.Id == Guid.Parse(timeSlotId));

                if (timeSlot == null || timeSlot.IsActive == false)
                    throw new ErrorResponse(404, (int)TimeSlotErrorEnums.TIMESLOT_UNAVAILIABLE,
                        TimeSlotErrorEnums.TIMESLOT_UNAVAILIABLE.GetDisplayName());

                if (!Utils.CheckTimeSlot(timeSlot))
                    throw new ErrorResponse(400, (int)TimeSlotErrorEnums.OUT_OF_TIMESLOT,
                        TimeSlotErrorEnums.OUT_OF_TIMESLOT.GetDisplayName());
                #endregion

                var order = new OrderResponse()
                {
                    Id = Guid.NewGuid(),
                    OrderCode = DateTime.Now.ToString("ddMMyy_HHmm") + "-" + Utils.GenerateRandomCode() + "-" + customerId,
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

        public async Task<BaseResponseViewModel<CoOrderResponse>> DeletePartyOrder(string customerId, string partyCode)
        {
            try
            {
                var partyOrder = await _unitOfWork.Repository<Party>().GetAll()
                                               .Where(x => x.PartyCode == partyCode)
                                               .FirstOrDefaultAsync();
                if (partyOrder is null)
                    throw new ErrorResponse(400, (int)PartyErrorEnums.INVALID_CODE, PartyErrorEnums.INVALID_CODE.GetDisplayName());

                CoOrderResponse coOrder = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, partyCode);

                if (coOrder is null)
                    throw new ErrorResponse(400, (int)OrderErrorEnums.NOT_FOUND_COORDER, OrderErrorEnums.NOT_FOUND_COORDER.GetDisplayName());

                var partyMem = coOrder.PartyOrder.Find(x => x.Customer.Id == Guid.Parse(customerId));
                if (partyMem.Customer.IsAdmin is true)
                {
                    ServiceHelpers.GetSetDataRedis(RedisSetUpType.DELETE, partyCode);
                }
                else
                {
                    coOrder.PartyOrder.Remove(partyMem);
                    ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, partyCode, coOrder);
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