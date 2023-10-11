using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.DTO.Request.Box;
using FINE.Service.DTO.Request.Order;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Helpers;
using FINE.Service.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface ISimulateService
    {
        Task<BaseResponseViewModel<SimulateResponse>> SimulateOrder(SimulateRequest request);
        Task<BaseResponseViewModel<OrderResponse>> CreateOrderForSimulate(string customerId, CreateOrderRequest request);
        Task<BaseResponsePagingViewModel<SimulateOrderStatusResponse>> SimulateOrderStatusToFinish(SimulateOrderStatusRequest request);
        Task<BaseResponsePagingViewModel<SimulateOrderStatusResponse>> SimulateOrderStatusToFinishPrepare(SimulateOrderStatusRequest request);
        Task<BaseResponsePagingViewModel<SimulateOrderStatusResponse>> SimulateOrderStatusToDelivering(SimulateOrderStatusRequest request);
        Task<BaseResponsePagingViewModel<SimulateOrderStatusResponse>> SimulateOrderStatusToBoxStored(SimulateOrderStatusRequest request);
    }

    public class SimulateService : ISimulateService
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOrderService _orderService;
        private readonly IStaffService _staffService;
        private readonly IPaymentService _paymentService;
        private readonly IBoxService _boxService;
        private readonly OrderService _orderServices;

        public SimulateService(IMapper mapper, IUnitOfWork unitOfWork, IOrderService orderService, IStaffService staffService, IPaymentService paymentService, IBoxService boxService, OrderService orderServices)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _orderService = orderService;
            _staffService = staffService;
            _paymentService = paymentService;
            _boxService = boxService;
            _orderServices = orderServices;
        }

        public async Task<BaseResponseViewModel<OrderResponse>> CreateOrderForSimulate(string customerId, CreateOrderRequest request)
        {
            try
            {
                #region Timeslot
                var timeSlot = await _unitOfWork.Repository<TimeSlot>().FindAsync(x => x.Id == request.TimeSlotId);

                if (request.OrderType is OrderTypeEnum.OrderToday && !Utils.CheckTimeSlot(timeSlot))
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
                var station = await _unitOfWork.Repository<Station>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(request.StationId));

                #region Check station in timeslot have available box 
                var getAllBoxInStation = await _unitOfWork.Repository<Box>().GetAll()
                                                .Where(x => x.StationId == Guid.Parse(request.StationId))
                                                .OrderBy(x => x.CreateAt)
                                                .ToListAsync();

                var getOrderBox = await _unitOfWork.Repository<OrderBox>().GetAll()
                                  .Include(x => x.Order)
                                  .Include(x => x.Box)
                                  .Where(x => x.Order.TimeSlotId == request.TimeSlotId
                                      && x.Box.StationId == Guid.Parse(request.StationId)
                                      && x.Order.CheckInDate.Date == Utils.GetCurrentDatetime().Date)
                                  .ToListAsync();
                var availableBoxes = getAllBoxInStation.Where(x => !getOrderBox.Any(a => a.BoxId == x.Id)).ToList();

                if (availableBoxes.Count == 0)
                    throw new ErrorResponse(400, (int)StationErrorEnums.STATION_FULL,
                                            StationErrorEnums.STATION_FULL.GetDisplayName());
                #endregion

                var order = _mapper.Map<Data.Entity.Order>(request);
                order.CustomerId = Guid.Parse(customerId);
                order.CheckInDate = DateTime.Now;
                order.OrderStatus = (int)OrderStatusEnum.PaymentPending;

                #region Party (if have)
                if (request.PartyCode is not null)
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
                        var party = checkCode.FirstOrDefault(x => x.CustomerId == order.CustomerId);

                        var redisValue = await ServiceHelpers.GetSetDataRedis(RedisDbEnum.CoOrder, RedisSetUpType.GET, request.PartyCode, null);
                        CoOrderResponse coOrder = JsonConvert.DeserializeObject<CoOrderResponse>(redisValue);

                        if (coOrder != null)
                        {
                            coOrder.IsPayment = true;
                            await ServiceHelpers.GetSetDataRedis(RedisDbEnum.CoOrder, RedisSetUpType.SET, request.PartyCode, coOrder);
                        }
                        party.OrderId = order.Id;
                        party.UpdateAt = DateTime.Now;

                        await _unitOfWork.Repository<Party>().UpdateDetached(party);
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
                #region Add order to box
                string key = null;
                if (getOrderBox.Count == 0)
                    key = Utils.GenerateRandomCode(10);
                else
                {
                    key = getOrderBox.FirstOrDefault().Key;
                }
                var addOrderToBoxRequest = new AddOrderToBoxRequest()
                {
                    BoxId = availableBoxes.FirstOrDefault().Id,
                    OrderId = order.Id
                };
                var addOrderToBox = await _boxService.AddOrderToBox(order.StationId.ToString(), key, addOrderToBoxRequest);
                #endregion

                #region split order 
                _orderServices.SplitOrder(order);
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

        public async Task<BaseResponseViewModel<SimulateResponse>> SimulateOrder(SimulateRequest request)
        {
            try
            {
                var rand = new Random();
                if (!Guid.TryParse(request.TimeSlotId, out Guid checkGuid))
                {
                    throw new ErrorResponse(404, (int)TimeSlotErrorEnums.NOT_FOUND,
                      TimeSlotErrorEnums.NOT_FOUND.GetDisplayName());
                }
                var timeslot = await _unitOfWork.Repository<TimeSlot>().FindAsync(x => x.Id == Guid.Parse(request.TimeSlotId));
                if (timeslot == null)
                    throw new ErrorResponse(404, (int)TimeSlotErrorEnums.NOT_FOUND,
                       TimeSlotErrorEnums.NOT_FOUND.GetDisplayName());

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
                var getAllCustomer = await _unitOfWork.Repository<Customer>().GetAll()
                                            .ToListAsync();
                getAllCustomer = getAllCustomer.OrderBy(x => rand.Next()).ToList();
                int customerIndex = 0;

                #region Single Order
                if (request.SingleOrder is not null)
                {

                    for (int quantity = 1; quantity <= request.SingleOrder.TotalOrder; quantity++)
                    {
                        customerIndex = quantity - 1;
                        var customer = getAllCustomer.ElementAt(customerIndex);

                        var payload = new CreatePreOrderRequest
                        {
                            OrderType = OrderTypeEnum.OrderToday,
                            TimeSlotId = Guid.Parse(request.TimeSlotId),
                        };

                        int numberProductTake = rand.Next(1, 4);
                        int randomQuantity = rand.Next(1, 4);
                        var randomProduct = productInMenu.OrderBy(x => rand.Next());
                        payload.OrderDetails = randomProduct
                            .Take(numberProductTake)
                            .Select(x => new CreatePreOrderDetailRequest
                            {
                                ProductId = x.Id,
                                Quantity = randomQuantity
                            })
                            .ToList();
                        try
                        {
                            var rs = _orderService.CreatePreOrder(customer.Id.ToString(), payload).Result.Data;
                            //var rs = _orderService.CreatePreOrder("4873582B-52AF-4D9E-96D0-0C461018CF81", payload).Result.Data;
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
                                OrderType = (OrderTypeEnum)rs.OrderType,
                                TimeSlotId = Guid.Parse(request.TimeSlotId),
                                StationId = stationId.ToString(),
                                //StationId = "CF42E4AF-F08F-4CC7-B83F-DA449857B2D3",
                                PaymentType = PaymentTypeEnum.FineWallet,
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
                                var result = CreateOrderForSimulate(customer.Id.ToString(), payloadCreateOrder).Result.Data;
                                //var result = _orderService.CreateOrder("4873582B-52AF-4D9E-96D0-0C461018CF81", payloadCreateOrder).Result.Data;
                                var orderSuccess = new OrderSuccess
                                {
                                    Id = result.Id,
                                    OrderCode = result.OrderCode,
                                    Customer = result.Customer,
                                    //OrderDetails = result.OrderDetails,
                                    OrderDetails = _mapper.Map<List<OrderDetailResponse>, List<OrderSuccessOrderDetail>>
                                                (result.OrderDetails.ToList())
                                };
                                response.SingleOrderResult.OrderSuccess.Add(orderSuccess);
                            }
                            catch (Exception ex)
                            {
                                ErrorResponse err = (ErrorResponse)ex.InnerException;
                                var orderFail = new OrderFailed
                                {
                                    OrderCode = rs.OrderCode,
                                    Customer = rs.Customer,
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
                            var randomCustomer = getAllCustomer.OrderBy(x => rand.Next()).FirstOrDefault();
                            ErrorResponse err = (ErrorResponse)ex.InnerException;
                            var orderFail = new OrderFailed
                            {
                                OrderCode = DateTime.Now.ToString("ddMMyy_HHmm") + "-" + Utils.GenerateRandomCode(5) + "-" + randomCustomer.Id,
                                Customer = new CustomerOrderResponse
                                {
                                    Id = randomCustomer.Id,
                                    CustomerCode = randomCustomer.Name,
                                    Email = randomCustomer.Email,
                                    Name = randomCustomer.Name,
                                    Phone = randomCustomer.Phone,
                                },
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
                }
                #endregion

                #region CoOrder
                if (request.CoOrder is not null)
                {
                    for (int quantity = 1; quantity <= request.CoOrder.TotalOrder; quantity++)
                    {
                        var listCustomer = getAllCustomer
                       .OrderBy(x => rand.Next())
                       .Take((int)request.CoOrder.CustomerEach)
                       .ToList();
                        var openCoOrderCustomer = listCustomer.Take(1).FirstOrDefault();
                        var restCustomers = listCustomer.Skip(1).ToList();

                        var payloadPreOrder = new CreatePreOrderRequest
                        {
                            OrderType = OrderTypeEnum.OrderToday,
                            TimeSlotId = Guid.Parse(request.TimeSlotId),
                            PartyType = PartyOrderType.CoOrder
                        };

                        int numberProductTake = rand.Next(1, 4);
                        payloadPreOrder.OrderDetails = productInMenu
                            .OrderBy(x => rand.Next())
                            .Take(numberProductTake)
                            .Select(x => new CreatePreOrderDetailRequest
                            {
                                ProductId = x.Id,
                                Quantity = 1
                            })
                            .ToList();

                        var openCoOrder = await _orderService.OpenCoOrder(openCoOrderCustomer.Id.ToString(), payloadPreOrder);

                        try
                        {
                            foreach (var cus in restCustomers)
                            {
                                var joinCoOrder = await _orderService.JoinPartyOrder(cus.Id.ToString(), openCoOrder.Data.PartyCode);
                                var cusPayloadPreOrder = new CreatePreOrderRequest
                                {
                                    OrderType = OrderTypeEnum.OrderToday,
                                    TimeSlotId = Guid.Parse(request.TimeSlotId),
                                    PartyType = PartyOrderType.CoOrder
                                };

                                int newNumberProductTake = rand.Next(1, 4);
                                cusPayloadPreOrder.OrderDetails = productInMenu
                                    .OrderBy(x => rand.Next())
                                    .Take(newNumberProductTake)
                                    .Select(x => new CreatePreOrderDetailRequest
                                    {
                                        ProductId = x.Id,
                                        Quantity = 1
                                    })
                                    .ToList();
                                var addProduct = await _orderService.AddProductIntoPartyCode(cus.Id.ToString(), openCoOrder.Data.PartyCode, cusPayloadPreOrder);
                                var confirmCoOrder = await _orderService.FinalConfirmCoOrder(cus.Id.ToString(), openCoOrder.Data.PartyCode);
                            }


                            var preCoOrder = await _orderService.CreatePreCoOrder(openCoOrderCustomer.Id.ToString(), (OrderTypeEnum)payloadPreOrder.OrderType, openCoOrder.Data.PartyCode);

                            var stationId = station
                                                .OrderBy(x => rand.Next())
                                                .Select(x => x.Id)
                                                .FirstOrDefault();

                            var payloadCreateOrder = new CreateOrderRequest()
                            {
                                Id = preCoOrder.Data.Id,
                                OrderCode = preCoOrder.Data.OrderCode,
                                PartyCode = openCoOrder.Data.PartyCode,
                                TotalAmount = preCoOrder.Data.TotalAmount,
                                FinalAmount = preCoOrder.Data.FinalAmount,
                                TotalOtherAmount = preCoOrder.Data.TotalOtherAmount,
                                OrderType = (OrderTypeEnum)preCoOrder.Data.OrderType,
                                TimeSlotId = Guid.Parse(request.TimeSlotId),
                                StationId = stationId.ToString(),
                                PaymentType = PaymentTypeEnum.FineWallet,
                                IsPartyMode = true,
                                ItemQuantity = preCoOrder.Data.ItemQuantity,
                                Point = preCoOrder.Data.Point,
                                OrderDetails = preCoOrder.Data.OrderDetails.Select(detail => new CreateOrderDetail()
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
                                OtherAmounts = preCoOrder.Data.OtherAmounts
                            };
                            try
                            {
                                var result = await CreateOrderForSimulate(openCoOrderCustomer.Id.ToString(), payloadCreateOrder);

                                var orderSuccess = new OrderSuccess
                                {
                                    Id = result.Data.Id,
                                    OrderCode = result.Data.OrderCode,
                                    Customer = result.Data.Customer,
                                };
                                response.CoOrderOrderResult.OrderSuccess.Add(orderSuccess);
                            }
                            catch (Exception ex)
                            {
                                ErrorResponse err = (ErrorResponse)ex.InnerException;
                                var orderFail = new OrderFailed
                                {
                                    OrderCode = preCoOrder.Data.OrderCode,
                                    Customer = preCoOrder.Data.Customer,
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
                            var randomCustomer = listCustomer.OrderBy(x => rand.Next()).FirstOrDefault();
                            if (ex is ErrorResponse errorResponse)
                            {
                                ErrorResponse err = (ErrorResponse)ex.InnerException;
                                var orderFail = new OrderFailed
                                {
                                    OrderCode = DateTime.Now.ToString("ddMMyy_HHmm") + "-" + Utils.GenerateRandomCode(5) + "-" + randomCustomer.Id,
                                    Customer = new CustomerOrderResponse
                                    {
                                        Id = randomCustomer.Id,
                                        CustomerCode = randomCustomer.Name,
                                        Email = randomCustomer.Email,
                                        Name = randomCustomer.Name,
                                        Phone = randomCustomer.Phone,
                                    },
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
                                    OrderCode = "",
                                    Customer = new CustomerOrderResponse
                                    {
                                        Id = randomCustomer.Id,
                                        CustomerCode = randomCustomer.Name,
                                        Email = randomCustomer.Email,
                                        Name = randomCustomer.Name,
                                        Phone = randomCustomer.Phone,
                                    },
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

                    }
                }
                #endregion
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

        public async Task<BaseResponsePagingViewModel<SimulateOrderStatusResponse>> SimulateOrderStatusToFinish(SimulateOrderStatusRequest request)
        {
            try
            {
                var getAllOrder = await _unitOfWork.Repository<Data.Entity.Order>().GetAll()
                                        .OrderByDescending(x => x.CheckInDate)
                                        .Where(x => x.OrderStatus == (int)OrderStatusEnum.BoxStored)
                                        .ToListAsync();
                getAllOrder = getAllOrder.Take(request.TotalOrder).ToList();
                var getAllCustomer = await _unitOfWork.Repository<Customer>().GetAll().ToListAsync();
                List<SimulateOrderStatusResponse> response = new List<SimulateOrderStatusResponse>();

                foreach (var order in getAllOrder)
                {
                    var payload = new UpdateOrderStatusRequest()
                    {
                        OrderStatus = OrderStatusEnum.Finished,
                    };

                    var updateStatus = await _staffService.UpdateOrderStatus(order.Id.ToString(), payload);

                    var result = new SimulateOrderStatusResponse()
                    {
                        OrderCode = order.OrderCode,
                        ItemQuantity = order.ItemQuantity,
                        CustomerName = getAllCustomer.FirstOrDefault(x => x.Id == order.CustomerId).Name
                    };

                    response.Add(result);
                }

                return new BaseResponsePagingViewModel<SimulateOrderStatusResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Total = response.Count()
                    },
                    Data = response
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
        public async Task<BaseResponsePagingViewModel<SimulateOrderStatusResponse>> SimulateOrderStatusToFinishPrepare(SimulateOrderStatusRequest request)
        {
            try
            {
                var getAllOrder = await _unitOfWork.Repository<Data.Entity.Order>().GetAll()
                                        .OrderByDescending(x => x.CheckInDate)
                                        .Where(x => x.OrderStatus == (int)OrderStatusEnum.Processing)
                                        .ToListAsync();
                getAllOrder = getAllOrder.Take(request.TotalOrder).ToList();
                var getAllCustomer = await _unitOfWork.Repository<Customer>().GetAll().ToListAsync();
                List<SimulateOrderStatusResponse> response = new List<SimulateOrderStatusResponse>();

                foreach (var order in getAllOrder)
                {
                    var payload = new UpdateOrderStatusRequest()
                    {
                        OrderStatus = OrderStatusEnum.FinishPrepare,
                    };

                    var updateStatus = await _staffService.UpdateOrderStatus(order.Id.ToString(), payload);

                    var result = new SimulateOrderStatusResponse()
                    {
                        OrderCode = order.OrderCode,
                        ItemQuantity = order.ItemQuantity,
                        CustomerName = getAllCustomer.FirstOrDefault(x => x.Id == order.CustomerId).Name
                    };

                    response.Add(result);
                }

                return new BaseResponsePagingViewModel<SimulateOrderStatusResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Total = response.Count()
                    },
                    Data = response
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
        public async Task<BaseResponsePagingViewModel<SimulateOrderStatusResponse>> SimulateOrderStatusToDelivering(SimulateOrderStatusRequest request)
        {
            try
            {
                var getAllOrder = await _unitOfWork.Repository<Data.Entity.Order>().GetAll()
                                        .OrderByDescending(x => x.CheckInDate)
                                        .Where(x => x.OrderStatus == (int)OrderStatusEnum.FinishPrepare)
                                        .ToListAsync();
                getAllOrder = getAllOrder.Take(request.TotalOrder).ToList();
                var getAllCustomer = await _unitOfWork.Repository<Customer>().GetAll().ToListAsync();
                List<SimulateOrderStatusResponse> response = new List<SimulateOrderStatusResponse>();

                foreach (var order in getAllOrder)
                {
                    var payload = new UpdateOrderStatusRequest()
                    {
                        OrderStatus = OrderStatusEnum.Delivering,
                    };

                    var updateStatus = await _staffService.UpdateOrderStatus(order.Id.ToString(), payload);

                    var result = new SimulateOrderStatusResponse()
                    {
                        OrderCode = order.OrderCode,
                        ItemQuantity = order.ItemQuantity,
                        CustomerName = getAllCustomer.FirstOrDefault(x => x.Id == order.CustomerId).Name
                    };

                    response.Add(result);
                }

                return new BaseResponsePagingViewModel<SimulateOrderStatusResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Total = response.Count()
                    },
                    Data = response
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
        public async Task<BaseResponsePagingViewModel<SimulateOrderStatusResponse>> SimulateOrderStatusToBoxStored(SimulateOrderStatusRequest request)
        {
            try
            {
                var getAllOrder = await _unitOfWork.Repository<Data.Entity.Order>().GetAll()
                                        .OrderByDescending(x => x.CheckInDate)
                                        .Where(x => x.OrderStatus == (int)OrderStatusEnum.Delivering)
                                        .ToListAsync();
                getAllOrder = getAllOrder.Take(request.TotalOrder).ToList();
                var getAllCustomer = await _unitOfWork.Repository<Customer>().GetAll().ToListAsync();
                List<SimulateOrderStatusResponse> response = new List<SimulateOrderStatusResponse>();

                foreach (var order in getAllOrder)
                {
                    var payload = new UpdateOrderStatusRequest()
                    {
                        OrderStatus = OrderStatusEnum.BoxStored,
                    };

                    var updateStatus = await _staffService.UpdateOrderStatus(order.Id.ToString(), payload);

                    var result = new SimulateOrderStatusResponse()
                    {
                        OrderCode = order.OrderCode,
                        ItemQuantity = order.ItemQuantity,
                        CustomerName = getAllCustomer.FirstOrDefault(x => x.Id == order.CustomerId).Name
                    };

                    response.Add(result);
                    ServiceHelpers.GetSetDataRedisOrder(RedisSetUpType.DELETE, order.Id.ToString());
                }

                return new BaseResponsePagingViewModel<SimulateOrderStatusResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Total = response.Count()
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
