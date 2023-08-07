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

namespace FINE.Service.Service
{
    public interface IOrderService
    {
        Task<BaseResponseViewModel<OrderResponse>> GetOrderById(string customerId, string id);
        //Task<BaseResponsePagingViewModel<GenOrderResponse>> GetOrders(PagingRequest paging);
        Task<BaseResponsePagingViewModel<OrderResponse>> GetOrderByCustomerId(string id, PagingRequest paging);
        Task<BaseResponseViewModel<CoOrderResponse>> GetPartyOrder(string partyCode);
        Task<BaseResponseViewModel<OrderResponse>> CreatePreOrder(string customerId, CreatePreOrderRequest request);
        Task<BaseResponseViewModel<OrderResponse>> CreateOrder(string id, CreateOrderRequest request);
        Task<BaseResponseViewModel<CoOrderResponse>> OpenCoOrder(string customerId, CreatePreOrderRequest request);
        Task<BaseResponseViewModel<CoOrderResponse>> JoinPartyOrder(string customerId, string partyCode);
        Task<BaseResponseViewModel<CoOrderResponse>> AddProductIntoPartyCode(string customerId, string partyCode, CreatePreOrderRequest request);
        Task<BaseResponseViewModel<CoOrderPartyCard>> FinalConfirmCoOrder(string customerId, string partyCode);
        Task<BaseResponseViewModel<OrderResponse>> CreatePreCoOrder(string customerId, string timeSlotId, string partyCode);

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

                if (!Utils.CheckTimeSlot(timeSlot))
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
                #region Check data
                var timeSlot = await _unitOfWork.Repository<TimeSlot>().FindAsync(x => x.Id == request.TimeSlotId);

                if (timeSlot == null || timeSlot.IsActive == false)
                    throw new ErrorResponse(404, (int)TimeSlotErrorEnums.TIMESLOT_UNAVAILIABLE,
                        TimeSlotErrorEnums.TIMESLOT_UNAVAILIABLE.GetDisplayName());

                if (!Utils.CheckTimeSlot(timeSlot))
                    throw new ErrorResponse(400, (int)TimeSlotErrorEnums.OUT_OF_TIMESLOT,
                        TimeSlotErrorEnums.OUT_OF_TIMESLOT.GetDisplayName());

                var customer = await _unitOfWork.Repository<Customer>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(customerId));

                if (customer.Phone == null)
                    throw new ErrorResponse(400, (int)CustomerErrorEnums.MISSING_PHONENUMBER,
                                            CustomerErrorEnums.MISSING_PHONENUMBER.GetDisplayName());

                var station = await _unitOfWork.Repository<Station>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(customerId));
                #endregion

                var order = _mapper.Map<Data.Entity.Order>(request);
                order.CustomerId = Guid.Parse(customerId);
                order.CheckInDate = DateTime.Now;
                order.OrderStatus = (int)OrderStatusEnum.PaymentPending;

                await _unitOfWork.Repository<Data.Entity.Order>().InsertAsync(order);
                await _unitOfWork.CommitAsync();

                var isSuccessPayment = await _paymentService.CreatePayment(order, request.Point, request.PaymentType);
                if (isSuccessPayment == false)
                {
                    throw new ErrorResponse(400, (int)TransactionErrorEnum.CREATE_TRANS_FAIL,
                            TransactionErrorEnum.CREATE_TRANS_FAIL.GetDisplayName());
                }
                else
                {
                    order.OrderStatus = (int)OrderStatusEnum.Processing;
                    await _unitOfWork.Repository<Data.Entity.Order>().UpdateDetached(order);
                    await _unitOfWork.CommitAsync();
                }

                var resultOrder = _mapper.Map<OrderResponse>(order);
                resultOrder.Customer = _mapper.Map<CustomerOrderResponse>(customer);
                resultOrder.StationOrder = _mapper.Map<StationOrderResponse>(station);

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

        public async Task<BaseResponseViewModel<CoOrderResponse>> OpenCoOrder(string customerId, CreatePreOrderRequest request)
        {
            try
            {
                // Tạo kết nối
                ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost:6379 ,password=zaQ@1234");

                // Lấy DB
                IDatabase db = redis.GetDatabase(1);

                // Ping thử
                if (db.Ping().TotalSeconds > 5)
                {
                    throw new TimeoutException("Server Redis không hoạt động");
                }

                #region check timeslot
                var timeSlot = _unitOfWork.Repository<TimeSlot>().Find(x => x.Id == request.TimeSlotId);

                if (timeSlot == null || timeSlot.IsActive == false)
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
                    PartyOrder = new List<CoOrderPartyCard>()
                };

                var orderCard = new CoOrderPartyCard()
                {
                    Customer = _mapper.Map<CustomerCoOrderResponse>(customer),
                    OrderDetails = new List<CoOrderDetailResponse>()
                };
                orderCard.Customer.IsAdmin = true;

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

                var redisValue = JsonConvert.SerializeObject(coOrder);
                db.StringSet(coOrder.PartyCode, redisValue);

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
                redis.Close();
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

                // Tạo kết nối
                ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost:6379 ,password=zaQ@1234");

                // Lấy DB
                IDatabase db = redis.GetDatabase(1);

                // Ping thử
                if (db.Ping().TotalSeconds > 5)
                {
                    throw new TimeoutException("Server Redis không hoạt động");
                }

                var redisValue = db.StringGet(partyCode);
                var coOrder = JsonConvert.DeserializeObject<CoOrderResponse>(redisValue);

                var customer = await _unitOfWork.Repository<Customer>().GetAll()
                                    .FirstOrDefaultAsync(x => x.Id == Guid.Parse(customerId));

                var orderCard = new CoOrderPartyCard()
                {
                    Customer = _mapper.Map<CustomerCoOrderResponse>(customer)
                };
                coOrder.PartyOrder.Add(orderCard);

                var redisNewValue = JsonConvert.SerializeObject(coOrder);
                db.StringSet(partyCode, redisNewValue);

                var newParty = new Party()
                {
                    Id = Guid.NewGuid(),
                    CustomerId = Guid.Parse(customerId),
                    PartyCode = partyCode,
                    Status = (int)PartyOrderStatus.NotConfirm,
                    IsActive = true,
                    CreateAt = DateTime.Now,
                };

                _unitOfWork.Repository<Party>().InsertAsync(newParty);
                _unitOfWork.CommitAsync();
                redis.Close();
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

        public async Task<BaseResponseViewModel<CoOrderResponse>> GetPartyOrder(string partyCode)
        {
            try
            {

                var partyOrder = await _unitOfWork.Repository<Party>().GetAll()
                                                .Where(x => x.PartyCode == partyCode)
                                                .FirstOrDefaultAsync();
                if (partyOrder == null)
                    throw new ErrorResponse(400, (int)PartyErrorEnums.INVALID_CODE, PartyErrorEnums.INVALID_CODE.GetDisplayName());

                // Tạo kết nối
                ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost:6379 ,password=zaQ@1234");

                // Lấy DB
                IDatabase db = redis.GetDatabase(1);

                // Ping thử
                if (db.Ping().TotalSeconds > 5)
                {
                    throw new TimeoutException("Server Redis không hoạt động");
                }

                var redisValue = await db.StringGetAsync(partyCode);
                var coOrder = JsonConvert.DeserializeObject<CoOrderResponse>(redisValue);
                redis.Close();
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

        public async Task<BaseResponseViewModel<CoOrderResponse>> AddProductIntoPartyCode(string customerId, string partyCode, CreatePreOrderRequest request)
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

                var partyOrder = await _unitOfWork.Repository<Party>().GetAll()
                                                .Where(x => x.PartyCode == partyCode)
                                                .FirstOrDefaultAsync();
                if (partyOrder == null)
                    throw new ErrorResponse(400, (int)PartyErrorEnums.INVALID_CODE, PartyErrorEnums.INVALID_CODE.GetDisplayName());

                // Tạo kết nối
                ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost:6379 ,password=zaQ@1234");

                // Lấy DB
                IDatabase db = redis.GetDatabase(1);

                // Ping thử
                if (db.Ping().TotalSeconds > 5)
                {
                    throw new TimeoutException("Server Redis không hoạt động");
                }

                var redisValue = db.StringGet(partyCode);
                var coOrder = JsonConvert.DeserializeObject<CoOrderResponse>(redisValue);

                var orderCard = coOrder.PartyOrder.FirstOrDefault(x => x.Customer.Id == Guid.Parse(customerId));

                foreach (var requestOD in request.OrderDetails)
                {
                    if (requestOD.Quantity == 0)
                    {
                        var orderDetailCard = orderCard.OrderDetails.Find(x => x.ProductId == requestOD.ProductId);
                        orderCard.OrderDetails.Remove(orderDetailCard);

                        orderCard.ItemQuantity -= orderDetailCard.Quantity;
                        orderCard.TotalAmount -= orderDetailCard.TotalAmount;
                    }
                    else
                    {
                        var productInMenu = _unitOfWork.Repository<ProductInMenu>().GetAll()
                            .Include(x => x.Menu)
                            .Include(x => x.Product)
                            .Where(x => x.ProductId == requestOD.ProductId && x.Menu.TimeSlotId == timeSlot.Id)
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

                        if (orderCard.OrderDetails == null)
                        {
                            orderCard.OrderDetails = new List<CoOrderDetailResponse>();

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
                        else 
                        {
                            var orderDetailCard = orderCard.OrderDetails.Find(x => x.ProductId == requestOD.ProductId);
                            if (orderDetailCard != null)
                            {
                                var quantityAdding = requestOD.Quantity - orderDetailCard.Quantity;
                                var amountAdding = (requestOD.Quantity * productInMenu.Product.Price) - orderDetailCard.TotalAmount;

                                orderDetailCard.Quantity = requestOD.Quantity;
                                orderDetailCard.TotalAmount = (requestOD.Quantity * productInMenu.Product.Price);

                                orderCard.ItemQuantity += quantityAdding;
                                orderCard.TotalAmount += amountAdding;
                            }
                            else
                            {
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

                        }
                    }
                }

                var redisNewValue = JsonConvert.SerializeObject(coOrder);
                var rs = db.StringSet(partyCode, redisNewValue);
                redis.Close();
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

                ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost:6379 ,password=zaQ@1234");

                // Lấy DB
                IDatabase db = redis.GetDatabase(1);

                // Ping thử
                if (db.Ping().TotalSeconds > 5)
                {
                    throw new TimeoutException("Server Redis không hoạt động");
                }

                var redisValue = db.StringGet(partyCode);
                var coOrder = JsonConvert.DeserializeObject<CoOrderResponse>(redisValue);

                var orderCard = coOrder.PartyOrder.FirstOrDefault(x => x.Customer.Id == Guid.Parse(customerId));
                redis.Close();
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
                // Tạo kết nối
                ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost:6379 ,password=zaQ@1234");

                // Lấy DB
                IDatabase db = redis.GetDatabase(1);

                // Ping thử
                if (db.Ping().TotalSeconds > 5)
                {
                    throw new TimeoutException("Server Redis không hoạt động");
                }

                var redisValue = db.StringGet(partyCode);
                var coOrder = JsonConvert.DeserializeObject<CoOrderResponse>(redisValue);

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
                    OrderType = (int)OrderTypeEnum.Delivery,
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

        //public Task<BaseResponseViewModel<OrderResponse>> ConfirmCoOrder(string customerId,string orderId , CreatePreOrderRequest request)
        //{
        //    try
        //    {
        //        var partyOrder = _unitOfWork.Repository<Order>().GetAll()
        //                                   .Where()
        //                                   .FirstOrDefault();  
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //}

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

        //    public async Task<BaseResponseViewModel<dynamic>> OpenCoOrder(int customerId, CreateGenOrderRequest request)
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

