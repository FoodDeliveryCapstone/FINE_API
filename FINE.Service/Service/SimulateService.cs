using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Box;
using FINE.Service.DTO.Request.Order;
using FINE.Service.DTO.Request.Package;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Helpers;
using FINE.Service.Utilities;
using FirebaseAdmin.Messaging;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        Task<BaseResponsePagingViewModel<SimulateOrderStatusResponse>> SimulateOrderStatusToFinish(SimulateOrderStatusRequest request);
        Task<BaseResponseViewModel<SimulateOrderForStaffResponse>> SimulateOrderStatusToFinishPrepare(SimulateOrderStatusForStaffRequest request);
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
        private readonly IPackageService _packageService;

        public SimulateService(IMapper mapper, IUnitOfWork unitOfWork, IOrderService orderService, IStaffService staffService, IPaymentService paymentService, IBoxService boxService, IPackageService packageService)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _orderService = orderService;
            _staffService = staffService;
            _paymentService = paymentService;
            _boxService = boxService;
            _packageService = packageService;
        }
        #region Simulate CreateOrder
        public async void SplitOrderAndCreateOrderBox(Order order)
        {
            try
            {
                var keyOrder = RedisDbEnum.Box.GetDisplayName() + ":Order:" + order.OrderCode;
                List<Guid> listLockOrder = new List<Guid>();
                var redisLockValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyOrder, null);
                if (redisLockValue.HasValue == true)
                {
                    listLockOrder = JsonConvert.DeserializeObject<List<Guid>>(redisLockValue);
                }
                if (order.IsPartyMode == true)
                {

                }
                else
                {
                    var boxId = listLockOrder.FirstOrDefault();
                    var orderBox = new OrderBox()
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        BoxId = boxId,
                        Key = Utils.GenerateRandomCode(10),
                        Status = (int)OrderBoxStatusEnum.NotPicked,
                        CreateAt = DateTime.Now
                    };
                    _unitOfWork.Repository<OrderBox>().InsertAsync(orderBox);

                    List<PackageOrderDetailModel> packageOrderDetails = new List<PackageOrderDetailModel>();
                    PackageResponse packageResponse;

                    HashSet<KeyValuePair<Guid, string>> listStoreId = new HashSet<KeyValuePair<Guid, string>>();
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

                        foreach (var orderDetail in listOdByStore)
                        {
                            var productInMenu = _unitOfWork.Repository<ProductInMenu>().GetAll().FirstOrDefault(x => x.Id == orderDetail.ProductInMenuId);
                            var productTotalDetail = packageResponse.ProductTotalDetails.Find(x => x.ProductInMenuId == orderDetail.ProductInMenuId);

                            packageOrderDetails.Add(new PackageOrderDetailModel()
                            {
                                ProductId = productInMenu.ProductId,
                                ProductInMenuId = orderDetail.ProductInMenuId,
                                Quantity = orderDetail.Quantity,
                                ErrorQuantity = 0,
                                IsReady = false
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
                                productTotalDetail.ProductDetails.Add(new ProductDetail()
                                {
                                    OrderId = order.Id,
                                    OrderCode = order.OrderCode,
                                    StationId = (Guid)order.StationId,
                                    BoxId = orderBox.BoxId,
                                    CheckInDate = order.CheckInDate,
                                    Quantity = orderDetail.Quantity,
                                    IsFinishPrepare = false,
                                    IsAssignToShipper = false
                                });
                                packageResponse.ProductTotalDetails.Add(productTotalDetail);
                            }
                            else
                            {
                                productTotalDetail.ProductDetails.Add(new ProductDetail()
                                {
                                    OrderId = order.Id,
                                    OrderCode = order.OrderCode,
                                    StationId = (Guid)order.StationId,
                                    BoxId = orderBox.BoxId,
                                    CheckInDate = order.CheckInDate,
                                    Quantity = orderDetail.Quantity,
                                    IsFinishPrepare = false
                                });
                                productTotalDetail.PendingQuantity += orderDetail.Quantity;
                            }
                            packageResponse.TotalProductInDay += orderDetail.Quantity;
                            packageResponse.TotalProductPending += orderDetail.Quantity;

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
                        ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, keyOrder, packageOrderDetails);
                    }
                }
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
        public async Task<BaseResponseViewModel<OrderResponse>> CreateOrderForSimulate(string customerId, CreateOrderRequest request)
        {
            try
            {
                #region Timeslot
                var timeSlot = await _unitOfWork.Repository<TimeSlot>().FindAsync(x => x.Id == request.TimeSlotId);

                if (request.OrderType is OrderTypeEnum.OrderToday && !Utils.CheckTimeSlot(timeSlot) && timeSlot.Id != Guid.Parse("E8D529D4-6A51-4FDB-B9DB-E29F54C0486E"))
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

        #endregion
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

                SimulateResponse response = new SimulateResponse()
                {
                    SingleOrderResult = new SimulateSingleOrderResponse()
                    {
                        OrderSuccess = new List<OrderSimulateResponse>(),
                        OrderFailed = new List<OrderSimulateResponse>()
                    },
                    CoOrderOrderResult = new SimulateCoOrderResponse()
                    {
                        OrderSuccess = new List<OrderSimulateResponse>(),
                        OrderFailed = new List<OrderSimulateResponse>()
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
                var store = await _unitOfWork.Repository<Store>().GetAll().ToListAsync();

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
                            var rs = await _orderService.CreatePreOrder(customer.Id.ToString(), payload);
                            //var rs = _orderService.CreatePreOrder("4873582B-52AF-4D9E-96D0-0C461018CF81", payload).Result.Data;
                            var stationId = station
                                                .OrderBy(x => rand.Next())
                                                .Select(x => x.Id)
                                                .FirstOrDefault();

                            var payloadCreateOrder = new CreateOrderRequest()
                            {
                                Id = rs.Data.Id,
                                OrderCode = rs.Data.OrderCode,
                                PartyCode = null,
                                TotalAmount = rs.Data.TotalAmount,
                                FinalAmount = rs.Data.FinalAmount,
                                TotalOtherAmount = rs.Data.TotalOtherAmount,
                                OrderType = (OrderTypeEnum)rs.Data.OrderType,
                                TimeSlotId = Guid.Parse(request.TimeSlotId),
                                //StationId = stationId.ToString(),
                                StationId = "C0AB11B3-7B05-41DF-9B23-1427481415F4",
                                PaymentType = PaymentTypeEnum.FineWallet,
                                IsPartyMode = false,
                                ItemQuantity = rs.Data.ItemQuantity,
                                Point = rs.Data.Point,
                                OrderDetails = rs.Data.OrderDetails.Select(detail => new CreateOrderDetail()
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
                                OtherAmounts = rs.Data.OtherAmounts
                            };

                            try
                            {
                                var result = await CreateOrderForSimulate(customer.Id.ToString(), payloadCreateOrder);
                                //var result = _orderService.CreateOrder("4873582B-52AF-4D9E-96D0-0C461018CF81", payloadCreateOrder).Result.Data;
                                var orderSuccess = new OrderSimulateResponse
                                {
                                    CustomerName = result.Data.Customer.Name,
                                    Message = "Success",
                                };
                                var listOrderDetails = _mapper.Map<List<OrderDetailResponse>, List<OrderSuccessOrderDetail>>
                                                (result.Data.OrderDetails.ToList());
                                var groupedDetails = listOrderDetails
                                   .GroupBy(detail => detail.StoreId)
                                   .Select(group => new GroupedOrderDetail
                                   {
                                       StoreId = group.Key,
                                       StoreName = store.FirstOrDefault(x => x.Id == group.Key).StoreName,
                                       ProductAndQuantity = group.Select(detail => new ProductAndQuantity
                                       {
                                           ProductName = detail.ProductName,
                                           Quantity = detail.Quantity
                                       }).ToList()
                                   }).ToList();

                                orderSuccess.OrderDetails = groupedDetails;

                                response.SingleOrderResult.OrderSuccess.Add(orderSuccess);
                            }
                            catch (ErrorResponse ex)
                            {
                                var orderFail = new OrderSimulateResponse
                                {
                                    CustomerName = rs.Data.Customer.Name,
                                    Message = ex.Error.Message
                                };
                                response.SingleOrderResult.OrderFailed.Add(orderFail);
                            }
                        }
                        catch (ErrorResponse ex)
                        {
                            var randomCustomer = getAllCustomer.OrderBy(x => rand.Next()).FirstOrDefault();
                            var orderFail = new OrderSimulateResponse
                            {
                                CustomerName = randomCustomer.Name,
                                Message = ex.Error.Message
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
                        var openCoOrderCustomer = listCustomer.FirstOrDefault();
                        var restCustomers = listCustomer.Skip(1).ToList();

                        var payloadPreOrder = new CreatePreOrderRequest
                        {
                            OrderType = OrderTypeEnum.OrderToday,
                            TimeSlotId = Guid.Parse(request.TimeSlotId),
                            PartyType = PartyOrderType.CoOrder
                        };

                        payloadPreOrder.OrderDetails = productInMenu
                            .OrderBy(x => rand.Next())
                            .Take(1)
                            .Select(x => new CreatePreOrderDetailRequest
                            {
                                ProductId = x.Id,
                                Quantity = 1
                            })
                            .ToList();

                        var openCoOrder = await _orderService.OpenParty(openCoOrderCustomer.Id.ToString(), payloadPreOrder);

                        try
                        {
                            foreach (var cus in restCustomers)
                            {
                                var joinCoOrder = await _orderService.JoinPartyOrder(cus.Id.ToString(), timeslot.Id.ToString(), openCoOrder.Data.PartyCode);
                                //var cusPayloadPreOrder = new AddProductToCardRequest
                                //{
                                //    TimeSlotId = request.TimeSlotId,
                                //    ProductId = productInMenu.FirstOrDefault().Id.ToString(),
                                //    Quantity = 1
                                //};

                                //cusPayloadPreOrder.Card = productInMenu
                                //    //.OrderBy(x => rand.Next())
                                //    .Take(1)
                                //    .Select(x => new ProductInCardRequest
                                //    {
                                //        ProductId = x.Id.ToString(),
                                //        Quantity = 1
                                //    })
                                //    .ToList();
                                var cusPayloadPreOrder = new CreatePreOrderRequest
                                {
                                    TimeSlotId = Guid.Parse(request.TimeSlotId),
                                    OrderType = OrderTypeEnum.OrderToday,
                                    PartyType = PartyOrderType.CoOrder,
                                    PartyCode = openCoOrder.Data.PartyCode
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
                                //StationId = "CF42E4AF-F08F-4CC7-B83F-DA449857B2D3",
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

                                var orderSuccess = new OrderSimulateResponse
                                {
                                    CustomerName = result.Data.Customer.Name,
                                    Message = "Success",
                                };
                                var listOrderDetails = _mapper.Map<List<OrderDetailResponse>, List<OrderSuccessOrderDetail>>
                                                (result.Data.OrderDetails.ToList());
                                var groupedDetails = listOrderDetails
                                   .GroupBy(detail => detail.StoreId)
                                   .Select(group => new GroupedOrderDetail
                                   {
                                       StoreId = group.Key,
                                       StoreName = store.FirstOrDefault(x => x.Id == group.Key).StoreName,
                                       ProductAndQuantity = group.Select(detail => new ProductAndQuantity
                                       {
                                           ProductName = detail.ProductName,
                                           Quantity = detail.Quantity
                                       }).ToList()
                                   }).ToList();

                                orderSuccess.OrderDetails = groupedDetails;

                                response.CoOrderOrderResult.OrderSuccess.Add(orderSuccess);
                            }
                            catch (ErrorResponse ex)
                            {
                                var orderFail = new OrderSimulateResponse
                                {
                                    CustomerName = preCoOrder.Data.Customer.Name,
                                    Message = ex.Error.Message
                                };
                                response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                            }
                        }
                        catch (ErrorResponse ex)
                        {
                            var randomCustomer = listCustomer.OrderBy(x => rand.Next()).FirstOrDefault();
                            var orderFail = new OrderSimulateResponse
                            {
                                CustomerName = randomCustomer.Name,
                                Message = ex.Error.Message
                            };
                            response.CoOrderOrderResult.OrderFailed.Add(orderFail);
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
        public async Task<BaseResponseViewModel<SimulateOrderForStaffResponse>> SimulateOrderStatusToFinishPrepare(SimulateOrderStatusForStaffRequest request)
        {
            try
            {
                var getAllStaff = await _unitOfWork.Repository<Staff>().GetAll().Where(x => x.StoreId != null).ToListAsync();
                SimulateOrderForStaffResponse response = new SimulateOrderForStaffResponse()
                {
                    OrderSuccess = new List<SimulateOrderForStaff>(),
                    OrderFailed = new List<SimulateOrderForStaff>()
                };
                foreach (var staff in getAllStaff)
                {
                    var getOrderInStore = await _packageService.GetPackage(staff.Id.ToString(), request.TimeSlotId.ToString());

                    if (getOrderInStore.Data.TotalProductPending != 0)
                    {
                        //lấy ds sản phẩm bị thiếu
                        string jsonFilePath = "Configuration\\listProductsMissingInSimulate.json"; 
                        string jsonString = File.ReadAllText(jsonFilePath);
                        var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(jsonString);
                        var productIdsMissing = jsonObject["ProductIdsMissing"];

                        foreach (var product in getOrderInStore.Data.ProductTotalDetails)
                        {
                            if(productIdsMissing.Any(x => x == product.ProductId.ToString()))
                            {
                                List<string> listProductId = new List<string>
                                {
                                    product.ProductId.ToString()
                                };

                                var updatePackagePayload = new UpdateProductPackageRequest
                                {
                                    timeSlotId = request.TimeSlotId.ToString(),
                                    Type = PackageUpdateTypeEnum.Error,
                                    Quantity = product.PendingQuantity,
                                    ProductsUpdate = listProductId
                                };

                                var updatePackage = await _packageService.UpdatePackage(staff.Id.ToString(), updatePackagePayload);

                                var orderFail = new SimulateOrderForStaff()
                                {
                                    StoreId = staff.StoreId,
                                    StaffName = staff.Name,
                                    ProductName = product.ProductName,
                                    Quantity = product.PendingQuantity,
                                    Message = "This product is not avaliable!",
 
                                };

                                response.OrderFailed.Add(orderFail);
                            }
                            else
                            {
                                //Bỏ qua product có pending quantity = 0
                                if (product.PendingQuantity == 0)
                                { }
                                else
                                {
                                    List<string> listProductId = new List<string>
                                    {
                                        product.ProductId.ToString()
                                    };

                                    var updatePackagePayload = new UpdateProductPackageRequest
                                    {
                                        timeSlotId = request.TimeSlotId.ToString(),
                                        Type = PackageUpdateTypeEnum.Confirm,
                                        Quantity = product.PendingQuantity,
                                        ProductsUpdate = listProductId
                                    };

                                    var updatePackage = await _packageService.UpdatePackage(staff.Id.ToString(), updatePackagePayload);

                                    var orderSuccess = new SimulateOrderForStaff()
                                    {
                                        Message = "Success",
                                        StoreId = staff.StoreId,
                                        StaffName = staff.Name,
                                        ProductName = product.ProductName,
                                        Quantity = product.PendingQuantity
                                    };

                                    response.OrderSuccess.Add(orderSuccess);
                                }
                            }
                        }
                    }
                }

                return new BaseResponseViewModel<SimulateOrderForStaffResponse>()
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
