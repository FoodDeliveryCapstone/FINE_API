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
        Task<BaseResponseViewModel<SimulateOrderForStaffAndShipperResponse>> SimulateOrderStatusToFinishPrepare(string timeslotId);
        Task<BaseResponseViewModel<SimulateOrderForStaffAndShipperResponse>> SimulateOrderStatusToDelivering(string timeslotId);
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
        private readonly IStationService _stationService;

        public SimulateService(IMapper mapper, IUnitOfWork unitOfWork, IOrderService orderService, IStaffService staffService, IPaymentService paymentService, IBoxService boxService, IPackageService packageService, IStationService stationService)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _orderService = orderService;
            _staffService = staffService;
            _paymentService = paymentService;
            _boxService = boxService;
            _packageService = packageService;
            _stationService = stationService;
        }
        #region Simulate CreateOrder
        public async Task SplitOrderAndCreateOrderBox(Order order, string? partyCode = null)
        {
            try
            {
                #region lấy boxId đã lock
                var keyOrderBox = RedisDbEnum.Box.GetDisplayName() + ":Order:" + order.OrderCode;

                List<Guid> listLockOrder = new List<Guid>();
                var redisLockValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyOrderBox, null);
                if (redisLockValue.HasValue == true)
                {
                    listLockOrder = JsonConvert.DeserializeObject<List<Guid>>(redisLockValue);
                }
                #endregion

                #region lưu đơn vào tủ
                //mỗi order có 1 package order riêng
                PackageOrderResponse packageOrder = new PackageOrderResponse()
                {
                    TotalConfirm = 0,
                    NumberHasConfirm = 0,
                    NumberCannotConfirm = 0,
                    PackageOrderBoxes = new List<PackageOrderBoxModel>()
                };
                HashSet<KeyValuePair<Guid, string>> listBox = new HashSet<KeyValuePair<Guid, string>>();
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

                #region Ghi nhận order và cac product trong tủ
                if (!partyCode.IsNullOrEmpty())
                {
                    packageOrder.PartyCode = partyCode;
                    var party = _unitOfWork.Repository<Party>().GetAll().FirstOrDefault(x => x.PartyCode == partyCode);
                    var keyCoOrder = RedisDbEnum.CoOrder.GetDisplayName() + ":" + party.PartyCode;
                    var redisValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyCoOrder, null);
                    CoOrderResponse coOrder = JsonConvert.DeserializeObject<CoOrderResponse>(redisValue);

                    foreach (var partyOrder in coOrder.PartyOrder)
                    {
                        var item = packageOrder.PackageOrderBoxes.FirstOrDefault(x => x.PackageOrderDetailModels.IsNullOrEmpty());
                        item.PackageOrderDetailModels = partyOrder.OrderDetails.Select(x => new PackageOrderDetailModel
                        {
                            ProductId = x.ProductId,
                            ProductName = x.ProductName,
                            ProductInMenuId = x.ProductInMenuId,
                            Quantity = x.Quantity,
                            IsInBox = false
                        }).ToList();
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
                            Quantity = product.Quantity,
                            IsInBox = false
                        });
                    }
                }
                #endregion

                //xử lý đơn 
                HashSet<KeyValuePair<Guid, string>> listStoreId = new HashSet<KeyValuePair<Guid, string>>();
                PackageStaffResponse packageStaffResponse;

                //lấy list storeId từ orderdetail
                foreach (var od in order.OrderDetails)
                {
                    var store = _unitOfWork.Repository<Store>().GetAll().FirstOrDefault(x => x.Id == od.StoreId);
                    listStoreId.Add(new KeyValuePair<Guid, string>(store.Id, store.StoreName));
                }

                foreach (var storeId in listStoreId)
                {
                    var keyStaff = RedisDbEnum.Staff.GetDisplayName() + ":" + storeId.Value + ":" + order.TimeSlot.ArriveTime.ToString(@"hh\-mm\-ss");
                    var redisValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyStaff, null);

                    if (redisValue.HasValue == false)
                    {
                        packageStaffResponse = new PackageStaffResponse()
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
                        packageStaffResponse = JsonConvert.DeserializeObject<PackageStaffResponse>(redisValue);
                    }

                    var listOdByStore = order.OrderDetails.Where(x => x.StoreId == storeId.Key).ToList();
                    // xử lý ProductTotalDetail trong packageStaffResponse
                    foreach (var orderDetail in listOdByStore)
                    {
                        var productInMenu = _unitOfWork.Repository<ProductInMenu>().GetAll().FirstOrDefault(x => x.Id == orderDetail.ProductInMenuId);

                        var productTotalDetail = packageStaffResponse.ProductTotalDetails.Find(x => x.ProductInMenuId == orderDetail.ProductInMenuId);
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
                            packageStaffResponse.ProductTotalDetails.Add(productTotalDetail);
                        }
                        else
                        {
                            productTotalDetail.PendingQuantity += orderDetail.Quantity;
                        }

                        productTotalDetail = packageStaffResponse.ProductTotalDetails.FirstOrDefault(x => x.ProductId == productInMenu.ProductId);

                        //xử lý tới product này có trong những order nào số lượng là bao nhiêu
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
                        packageStaffResponse.TotalProductInDay += orderDetail.Quantity;
                        packageStaffResponse.TotalProductPending += orderDetail.Quantity;

                        //xử lý PackageStationResponse
                        if (packageStaffResponse.PackageStations is null)
                        {
                            packageStaffResponse.PackageStations = new List<PackageStationResponse>();
                        }
                        var stationPack = packageStaffResponse.PackageStations.FirstOrDefault(x => x.StationId == order.StationId && x.IsShipperAssign == false);
                        if (stationPack is null)
                        {
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
                            packageStaffResponse.PackageStations.Add(stationPackage);
                        }
                        else
                        {
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
                        packageOrder.TotalConfirm += orderDetail.Quantity;
                    }
                    ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, keyStaff, packageStaffResponse);
                }

                #region nhả lại số box đã lock
                var numberBox = listLockOrder.Count();
                var key = RedisDbEnum.Box.GetDisplayName() + ":Station";

                List<LockBoxinStationModel> listStationLockBox = new List<LockBoxinStationModel>();
                var redisStationValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, key, null);
                if (redisStationValue.HasValue == true)
                {
                    listStationLockBox = JsonConvert.DeserializeObject<List<LockBoxinStationModel>>(redisStationValue);
                }

                foreach (var station in listStationLockBox)
                {
                    station.NumberBoxLockPending -= numberBox;
                    station.ListBoxId = station.ListBoxId.Except(listLockOrder).ToList();
                    station.ListOrderBox.RemoveAll(x => x.Key == order.OrderCode);

                    listStationLockBox = listStationLockBox.Select(x => new LockBoxinStationModel
                    {
                        StationName = x.StationName,
                        StationId = x.StationId,
                        NumberBoxLockPending = x.NumberBoxLockPending - numberBox,
                    }).ToList();
                }

                await ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, key, listStationLockBox);
                await ServiceHelpers.GetSetDataRedis(RedisSetUpType.DELETE, keyOrderBox, null);
                #endregion

                _unitOfWork.Commit();
                var keyOrderPack = RedisDbEnum.OrderOperation.GetDisplayName() + ":" + order.OrderCode;
                ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, keyOrderPack, packageOrder);

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<BaseResponseViewModel<OrderResponse>> CreateOrderForSimulate(string customerId, CreateOrderRequest request)
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
                order.OrderStatus = (int)OrderStatusEnum.Processing;

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
                        request.PartyCode = null;
                    }
                    order.IsPartyMode = true;
                }
                #endregion

                await _paymentService.CreatePayment(order, request.Point, request.PaymentType);

                await _unitOfWork.Repository<Order>().InsertAsync(order);

                #region split order + create order box
                try
                {
                    await SplitOrderAndCreateOrderBox(order, request.PartyCode);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                #endregion
              
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
                var station = await _unitOfWork.Repository<Station>().GetAll().Where(x => x.IsAvailable == true).ToListAsync();
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

                        int numberProductTake = rand.Next(1, 3);
                        int randomQuantity = rand.Next(1, 3);
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
                                StationId = stationId.ToString(),
                                //StationId = "C0AB11B3-7B05-41DF-9B23-1427481415F4",
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

                            var getStationForDestination = await _stationService.GetStationByDestinationForOrder(timeslot.DestinationId.ToString(), rs.Data.OrderCode, 1);
                            var lockBox = await _stationService.LockBox(stationId.ToString(), rs.Data.OrderCode, 1);                            

                            try
                            {
                                var result = await CreateOrderForSimulate(customer.Id.ToString(), payloadCreateOrder);
                                //var updateLockBox = await _stationService.UpdateLockBox(LockBoxUpdateTypeEnum.Delete, rs.Data.OrderCode, null);
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
        public async Task<BaseResponseViewModel<SimulateOrderForStaffAndShipperResponse>> SimulateOrderStatusToFinishPrepare(string timeslotId)
        {
            try
            {
                //var getAllStaff = _unitOfWork.Repository<Staff>().GetAll().Where(x => x.StoreId != null).ToList();
                var timeSlot = _unitOfWork.Repository<TimeSlot>().GetAll().FirstOrDefault(x => x.Id == Guid.Parse(timeslotId));
                SimulateOrderForStaffAndShipperResponse response = new SimulateOrderForStaffAndShipperResponse()
                {
                    OrderSuccess = new List<SimulateOrderForStaffAndShipper>(),
                    OrderFailed = new List<SimulateOrderForStaffAndShipper>()
                };
                List<SimulateOrderForStaff> success = new List<SimulateOrderForStaff>();
                List<string> listProductPassioId = new List<string>();
                List<string> listProduct711Id = new List<string>();
                List<string> listProductLahaId = new List<string>();
                #region Passio
                var keyPassio = RedisDbEnum.Staff.GetDisplayName() + ":" + "Passio" + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");

                var redisPassioValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyPassio, null);
                if (redisPassioValue.HasValue == true)
                {
                    PackageStaffResponse getPassioPackage = JsonConvert.DeserializeObject<PackageStaffResponse>(redisPassioValue);

                    if (getPassioPackage.TotalProductPending != 0)
                    {
                        foreach (var product in getPassioPackage.ProductTotalDetails)
                        {
                            //Bỏ qua product có pending quantity = 0
                            if (product.PendingQuantity == 0)
                            { }
                            else
                            {
                                listProductPassioId.Add(product.ProductId.ToString());

                                var orderSuccess = new SimulateOrderForStaff()
                                {
                                    Message = "Success",
                                    StoreId = Guid.Parse("8DB35955-BBC5-40FB-B638-CB44AC786519"),
                                    StoreName = "Passio",
                                    StaffName = "Dương Minh",
                                    ProductName = product.ProductName,
                                    Quantity = product.PendingQuantity
                                };

                                success.Add(orderSuccess);
                            }
                        }

                    }

                    var updatePackagePassioPayload = new UpdateProductPackageRequest
                    {
                        TimeSlotId = timeslotId,
                        Type = PackageUpdateTypeEnum.Confirm,
                        StoreId = "8DB35955-BBC5-40FB-B638-CB44AC786519",
                        //Quantity = product.PendingQuantity,
                        ProductsUpdate = listProductPassioId
                    };
                    var updatePassioPackage = _packageService.UpdatePackage("5E67163F-80BE-4AF5-AD71-980388987695", updatePackagePassioPayload);
                    foreach (var station in getPassioPackage.PackageStations)
                    {
                        if (station.IsShipperAssign == false)
                        {
                            await Task.Delay(3000);
                            var confirmPassioDelivery = await _packageService.ConfirmReadyToDelivery("5E67163F-80BE-4AF5-AD71-980388987695", timeslotId, station.StationId.ToString());
                        }
                    }

                }
                #endregion

                #region 711
                var key711 = RedisDbEnum.Staff.GetDisplayName() + ":" + "7-Eleven" + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");

                var redis711Value = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, key711, null);
                if (redis711Value.HasValue == true)
                {
                    PackageStaffResponse get711Package = JsonConvert.DeserializeObject<PackageStaffResponse>(redis711Value);

                    if (get711Package.TotalProductPending != 0)
                    {
                        foreach (var product in get711Package.ProductTotalDetails)
                        {
                            //Bỏ qua product có pending quantity = 0
                            if (product.PendingQuantity == 0)
                            { }
                            else
                            {
                                listProduct711Id.Add(product.ProductId.ToString());

                                var orderSuccess = new SimulateOrderForStaff()
                                {
                                    Message = "Success",
                                    StoreId = Guid.Parse("751A2190-D06C-4D5E-9C5A-08C33C3DB266"),
                                    StoreName = "7-Eleven",
                                    StaffName = "Minh",
                                    ProductName = product.ProductName,
                                    Quantity = product.PendingQuantity
                                };

                                success.Add(orderSuccess);
                            }
                        }

                    }

                    var updatePackage711Payload = new UpdateProductPackageRequest
                    {
                        TimeSlotId = timeslotId,
                        Type = PackageUpdateTypeEnum.Confirm,
                        StoreId = "751A2190-D06C-4D5E-9C5A-08C33C3DB266",
                        //Quantity = product.PendingQuantity,
                        ProductsUpdate = listProduct711Id
                    };
                    var update711Package = _packageService.UpdatePackage("719840C7-5EA9-4A34-81ED-22E52F474CD1", updatePackage711Payload);
                    foreach (var station in get711Package.PackageStations)
                    {
                        if (station.IsShipperAssign == false)
                        {
                            await Task.Delay(3000);
                            var confirm711Delivery = await _packageService.ConfirmReadyToDelivery("719840C7-5EA9-4A34-81ED-22E52F474CD1", timeslotId, station.StationId.ToString());
                        }
                    }
                }
                #endregion

                #region Laha
                var keyLaha = RedisDbEnum.Staff.GetDisplayName() + ":" + "Laha" + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");

                var redisLahaValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyLaha, null);
                if (redisLahaValue.HasValue == true)
                {
                    PackageStaffResponse getLahaPackage = JsonConvert.DeserializeObject<PackageStaffResponse>(redisLahaValue);

                    if (getLahaPackage.TotalProductPending != 0)
                    {
                        foreach (var product in getLahaPackage.ProductTotalDetails)
                        {
                            //Bỏ qua product có pending quantity = 0
                            if (product.PendingQuantity == 0)
                            { }
                            else
                            {
                                listProductLahaId.Add(product.ProductId.ToString());

                                var orderSuccess = new SimulateOrderForStaff()
                                {
                                    Message = "Success",
                                    StoreId = Guid.Parse("E19422E9-2C97-4C6E-8919-F4AE0FA739D5"),
                                    StoreName = "Laha",
                                    StaffName = "A Bảo",
                                    ProductName = product.ProductName,
                                    Quantity = product.PendingQuantity
                                };

                                success.Add(orderSuccess);
                            }
                        }

                    }

                    var updatePackageLahaPayload = new UpdateProductPackageRequest
                    {
                        TimeSlotId = timeslotId,
                        Type = PackageUpdateTypeEnum.Confirm,
                        StoreId = "E19422E9-2C97-4C6E-8919-F4AE0FA739D5",
                        //Quantity = product.PendingQuantity,
                        ProductsUpdate = listProductLahaId
                    };
                    var updateLahaPackage = _packageService.UpdatePackage("087B66B8-55DE-4422-99AA-96D4A5639889", updatePackageLahaPayload);
                    foreach (var station in getLahaPackage.PackageStations)
                    {
                        if (station.IsShipperAssign == false)
                        {
                            await Task.Delay(3000);
                            var confirmLahaDelivery = await _packageService.ConfirmReadyToDelivery("087B66B8-55DE-4422-99AA-96D4A5639889", timeslotId, station.StationId.ToString());
                        }
                    }
                }
                #endregion

                response.OrderSuccess = success
                            .GroupBy(detail => new { detail.StoreId, detail.StaffName, detail.StoreName })
                            .Select(group => new SimulateOrderForStaffAndShipper
                            {
                                Message = group.FirstOrDefault()?.Message,
                                StoreId = group.Key.StoreId,
                                StoreName = group.Key.StoreName,
                                StaffName = group.Key.StaffName,
                                ProductAndQuantities = group.Select(productAndQuantity => new ProductAndQuantity
                                {
                                    ProductName = productAndQuantity.ProductName,
                                    Quantity = productAndQuantity.Quantity ?? 0
                                }).ToList(),
                            })
                            .ToList();

                return new BaseResponseViewModel<SimulateOrderForStaffAndShipperResponse>()
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
        public async Task<BaseResponseViewModel<SimulateOrderForStaffAndShipperResponse>> SimulateOrderStatusToDelivering(string timeslotId)
        {
            try
            {
                SimulateOrderForStaffAndShipperResponse response = new SimulateOrderForStaffAndShipperResponse()
                {
                    OrderSuccess = new List<SimulateOrderForStaffAndShipper>(),
                    OrderFailed = new List<SimulateOrderForStaffAndShipper>()
                };

                var getAllStaff = await _unitOfWork.Repository<Staff>().GetAll().Where(x => x.StationId != null).ToListAsync();
                var timeslot = await _unitOfWork.Repository<TimeSlot>().GetAll().FirstOrDefaultAsync(x => x.Id == Guid.Parse(timeslotId));

                foreach (var staff in getAllStaff)
                {
                    var key = RedisDbEnum.Shipper.GetDisplayName() + ":" + staff.Station.Code + ":" + timeslot.ArriveTime.ToString(@"hh\-mm\-ss");

                    var redisShipperValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, key, null);

                    if (redisShipperValue.HasValue == true)
                    {
                        PackageShipperResponse packageShipperResponse = JsonConvert.DeserializeObject<PackageShipperResponse>(redisShipperValue);

                        if (packageShipperResponse != null && packageShipperResponse.PackageStoreShipperResponses is not null)
                        {
                            foreach (var item in packageShipperResponse.PackageStoreShipperResponses)
                            {
                                if (item.IsTaken == false)
                                {
                                    try
                                    {
                                        var confirmPackage = await _packageService.ConfirmTakenPackage(staff.Id.ToString(), timeslotId, item.StoreId.ToString());

                                        var orderSuccess = new SimulateOrderForStaffAndShipper()
                                        {
                                            Message = "Success",
                                            StoreId = item.StoreId,
                                            StaffName = staff.Name,
                                            StationName = staff.Station.Name,
                                            ProductAndQuantities = _mapper.Map<List<PackStationDetailGroupByProduct>, List<ProductAndQuantity>>
                                                (item.PackStationDetailGroupByProducts)
                                        };
                                        response.OrderSuccess.Add(orderSuccess);
                                    }
                                    catch (Exception ex)
                                    {
                                        var orderFail = new SimulateOrderForStaffAndShipper()
                                        {
                                            StoreId = item.StoreId,
                                            StaffName = staff.Name,
                                            Message = "Fail to take package",
                                        };

                                        response.OrderFailed.Add(orderFail);
                                    }
                                }
                            }

                        }
                    }
                }

                return new BaseResponseViewModel<SimulateOrderForStaffAndShipperResponse>()
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
            catch (Exception ex)
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
