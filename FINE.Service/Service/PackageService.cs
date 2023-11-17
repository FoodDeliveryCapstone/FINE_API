using AutoMapper;
using Azure.Core;
using Castle.Core.Resource;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Attributes;
using FINE.Service.DTO.Request.Package;
using FINE.Service.DTO.Response;
using FINE.Service.Helpers;
using FINE.Service.Utilities;
using FirebaseAdmin.Messaging;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using static FINE.Service.Helpers.Enum;

namespace FINE.Service.Service
{
    public interface IPackageService
    {
        Task<BaseResponseViewModel<List<PackStationDetailGroupByBox>>> GetListBoxGroupByProduct(string staffId, string timeSlotId, string productId);
        Task<BaseResponseViewModel<PackageStaffResponse>> GetPackage(string staffId, string timeSlotId);
        Task<BaseResponseViewModel<List<PackageStationResponse>>> GetPackageGroupByStation(string staffId, string timeSlotId);
        Task<BaseResponseViewModel<PackageShipperResponse>> GetPackageForShipper(string staffId, string timeSlotId);
        Task<BaseResponseViewModel<PackageStaffResponse>> UpdatePackage(string staffId, UpdateProductPackageRequest request);
        Task<BaseResponseViewModel<dynamic>> ConfirmReadyToDelivery(string staffId, string timeSlotId, string stationId);
        Task<BaseResponseViewModel<dynamic>> ConfirmTakenPackage(string staffId, string timeSlotId, string storeId);
        Task<BaseResponseViewModel<dynamic>> ConfirmAllInBox(string staffId, string timeSlotId);
        Task<BaseResponseViewModel<dynamic>> ReportProductCannotRepair(string staffId, string timeslotId, string productId, SystemRoleTypeEnum memReport);
    }
    public class PackageService : IPackageService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IFirebaseMessagingService _fm;
        private readonly IProductService _productService;

        public PackageService(IUnitOfWork unitOfWork, IMapper mapper, IFirebaseMessagingService fm, IProductService productService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _fm = fm;
            _productService = productService;
        }

        public async Task<BaseResponseViewModel<dynamic>> ReportProductCannotRepair(string staffId, string timeslotId, string productId, SystemRoleTypeEnum memReport)
        {
            try
            {
                var staff = await _unitOfWork.Repository<Staff>().GetAll().FirstOrDefaultAsync(x => x.Id == Guid.Parse(staffId));
                var timeSlot = await _unitOfWork.Repository<TimeSlot>().GetAll().FirstOrDefaultAsync(x => x.Id == Guid.Parse(timeslotId));

                #region get redis
                var keyStaff = RedisDbEnum.Staff.GetDisplayName() + ":" + staff.Store.StoreName + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");
                var redisValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyStaff, null);
                PackageStaffResponse packageStaff = JsonConvert.DeserializeObject<PackageStaffResponse>(redisValue);
                #endregion

                var productError = packageStaff.ErrorProducts.FirstOrDefault(x => x.ReportMemType == (int)memReport && x.ProductId == Guid.Parse(productId) && x.IsRefuse == false);
                productError.IsRefuse = true;

                var listOrder = packageStaff.ProductTotalDetails.FirstOrDefault(x => x.ProductId == Guid.Parse(productId))
                                            .ProductDetails.Where(x => x.ErrorQuantity > 0).OrderByDescending(x => x.CheckInDate);

                var productDb = await _unitOfWork.Repository<ProductAttribute>().GetAll().FirstOrDefaultAsync(x => x.Id == Guid.Parse(productId));

                var errorNum = productError.Quantity;
                foreach (var errorOrder in listOrder)
                {
                    if (errorNum == 0) break;
                    int quantityErrorInOrder = 0;
                    var order = _unitOfWork.Repository<Order>().GetAll().FirstOrDefault(x => x.Id == errorOrder.OrderId);

                    var customerToken = _unitOfWork.Repository<Fcmtoken>().GetAll().FirstOrDefault(x => x.UserId == order.CustomerId).Token;
                    var packStation = packageStaff.PackageStations.FirstOrDefault(x => x.StationId == order.StationId && x.IsShipperAssign == false);

                    var keyOrder = RedisDbEnum.OrderOperation.GetDisplayName() + ":" + order.OrderCode;

                    var orderValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyOrder, null);
                    PackageOrderResponse packageOrder = JsonConvert.DeserializeObject<PackageOrderResponse>(orderValue);

                    var party = _unitOfWork.Repository<Party>().GetAll().Where(x => x.PartyCode == packageOrder.PartyCode).ToList();

                    if (errorNum >= errorOrder.ErrorQuantity)
                    {
                        quantityErrorInOrder = errorOrder.ErrorQuantity;
                        errorNum -= errorOrder.ErrorQuantity;
                        packageOrder.NumberCannotConfirm += errorOrder.ErrorQuantity;
                    }
                    else
                    {
                        errorOrder.ErrorQuantity += errorNum;
                        quantityErrorInOrder = errorNum;
                        errorNum = 0;
                    }

                    if (packageOrder.NumberCannotConfirm == packageOrder.TotalConfirm)
                    {
                        order.OrderStatus = (int)OrderStatusEnum.StaffCancel;
                        _unitOfWork.Repository<Order>().UpdateDetached(order);
                    }
                    else if (packageOrder.NumberHasConfirm != 0 && (packageOrder.NumberCannotConfirm + packageOrder.NumberHasConfirm == packageOrder.TotalConfirm))
                    {
                        packStation.ListOrder.Add(new KeyValuePair<Guid, string>(order.Id, order.OrderCode));
                        order.OrderStatus = (int)OrderStatusEnum.FinishPrepare;
                        _unitOfWork.Repository<Order>().UpdateDetached(order);
                    }
                    var numberError = quantityErrorInOrder;
                    var numberErrorOrder = 0;
                    var orderBox = packageOrder.PackageOrderBoxes.Where(x => x.PackageOrderDetailModels.Any(x => x.ProductId == Guid.Parse(productId))).ToList();

                    foreach (var box in orderBox)
                    {
                        if (numberError == 0) break;
                        var productInBox = box.PackageOrderDetailModels.FirstOrDefault(x => x.ProductId == Guid.Parse(productId));
                        if (productInBox.Quantity < numberError)
                        {
                            numberError -= productInBox.Quantity;
                            numberErrorOrder += productInBox.Quantity;
                            box.PackageOrderDetailModels.Remove(productInBox);
                        }
                        else
                        {
                            productInBox.Quantity -= numberError;
                            numberErrorOrder += numberError;
                            numberError = 0;
                        }
                    }

                    if (memReport == SystemRoleTypeEnum.StoreManager)
                    {
                        packStation.TotalQuantity -= quantityErrorInOrder;

                        var packMissing = packStation.ListPackageMissing.FirstOrDefault(x => x.ProductId == Guid.Parse(productId));
                        packMissing.Quantity -= quantityErrorInOrder;

                        if (packMissing.Quantity == 0)
                        {
                            packStation.ListPackageMissing.Remove(packMissing);
                        }
                    }
                    var refundAmount = productDb.Price * quantityErrorInOrder;

                    Notification notification = new Notification
                    {
                        Title = Constants.REPORT_ERROR_PACK,
                        //Body = 
                        Body = String.Format($"Có {quantityErrorInOrder} {productError.ProductName} đã hết hàng.{Environment.NewLine} Hệ thống sẽ hoàn lại " +
                                                        $"{refundAmount.ToString().Substring(0, refundAmount.ToString().Length - 3)}K vào ví của bạn sau khi đơn hàng " +
                                                        $"hoàn tất nhé!{Environment.NewLine} Cảm ơn bạn đã thông cảm cho FINE!")
                    };
                    var data = new Dictionary<string, string>()
                    {
                        { "type", NotifyTypeEnum.ForRefund.ToString()},
                        { "orderId", order.Id.ToString() }
                    };
                    BackgroundJob.Enqueue(() => _fm.SendToToken(customerToken, notification, data));

                    if (!party.IsNullOrEmpty())
                    {
                        party.OrderByDescending(x => x.CreateAt);

                        var keyCoOrder = RedisDbEnum.CoOrder.GetDisplayName() + ":" + packageOrder.PartyCode;
                        var redisCoOrderValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyCoOrder, null);

                        CoOrderResponse coOrder = JsonConvert.DeserializeObject<CoOrderResponse>(redisValue);

                        foreach (var member in party)
                        {
                            var partyOrder = coOrder.PartyOrder.FirstOrDefault(x => x.Customer.Id == member.CustomerId && x.OrderDetails.Any(x => x.ProductId == Guid.Parse(productId)));
                            if (partyOrder is not null)
                            {
                                var memberToken = _unitOfWork.Repository<Fcmtoken>().GetAll().FirstOrDefault(x => x.UserId == partyOrder.Customer.Id).Token;
                                if (numberErrorOrder == 0) break;
                                var productInParty = partyOrder.OrderDetails.FirstOrDefault(x => x.ProductId == Guid.Parse(productId));
                                if (productInParty.Quantity < numberErrorOrder)
                                {
                                    numberErrorOrder -= productInParty.Quantity;
                                }
                                else
                                {
                                    productInParty.Quantity -= numberErrorOrder;
                                    numberErrorOrder = 0;
                                }

                                Notification notificationMember = new Notification
                                {
                                    Title = Constants.REPORT_ERROR_PACK,
                                    //Body = 
                                    Body = String.Format($"Có {quantityErrorInOrder} {productError.ProductName} đã hết hàng.{Environment.NewLine} Hệ thống sẽ hoàn lại " +
                                                        $"{refundAmount.ToString().Substring(0, refundAmount.ToString().Length - 3)}K vào ví của chủ phòng sau khi đơn hàng " +
                                                        $"hoàn tất nhé!{Environment.NewLine} Cảm ơn bạn đã thông cảm cho FINE!")
                                };
                                BackgroundJob.Enqueue(() => _fm.SendToToken(memberToken, notificationMember, data));
                            }
                        }
                    }

                    var otherAmount = new OtherAmount()
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        Amount = refundAmount,
                        Type = (int)OtherAmountTypeEnum.Refund,
                        Note = $"Hoàn lại {refundAmount.ToString().Substring(0, refundAmount.ToString().Length - 3)}K. Lý do: {quantityErrorInOrder} món {productError.ProductName} đã hết hàng.",
                        Att1 = JsonConvert.SerializeObject(new KeyValuePair<Guid, int>(Guid.Parse(productId), quantityErrorInOrder))
                    };
                    _unitOfWork.Repository<OtherAmount>().Insert(otherAmount);
                    ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, keyOrder, packageOrder);
                }
                await _productService.UpdateProductCannotRepair(productId, false);

                _unitOfWork.Commit();
                ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, keyStaff, packageStaff);
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
        public async Task<BaseResponseViewModel<dynamic>> ConfirmTakenPackage(string staffId, string timeSlotId, string storeId)
        {
            try
            {
                PackageShipperResponse packageResponse = new PackageShipperResponse();

                var staff = await _unitOfWork.Repository<Staff>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(staffId));

                var timeSlot = await _unitOfWork.Repository<TimeSlot>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(timeSlotId));

                var key = RedisDbEnum.Shipper.GetDisplayName() + ":" + staff.Station.Code + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");

                var redisShipperValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, key, null);

                if (redisShipperValue.HasValue == true)
                {
                    packageResponse = JsonConvert.DeserializeObject<PackageShipperResponse>(redisShipperValue);
                }
                var packStore = packageResponse.PackageStoreShipperResponses.FirstOrDefault(x => x.StoreId == Guid.Parse(storeId) && x.IsTaken == false);

                foreach (var order in packStore.ListOrderId)
                {
                    var orderDb = await _unitOfWork.Repository<Order>().GetAll().FirstOrDefaultAsync(x => x.Id == order.Key);
                    orderDb.OrderStatus = (int)OrderStatusEnum.Delivering;

                    await _unitOfWork.Repository<Order>().UpdateDetached(orderDb);
                }
                var existPack = packageResponse.PackageStoreShipperResponses.FirstOrDefault(x => x.StoreId == Guid.Parse(storeId) && x.IsTaken == true && x.IsInBox == false);
                if (existPack is not null)
                {
                    existPack.TotalQuantity += packStore.TotalQuantity;
                    existPack.PackStationDetailGroupByProducts.AddRange(packStore.PackStationDetailGroupByProducts);

                    packageResponse.PackageStoreShipperResponses.Remove(packStore);
                }
                else
                {
                    packStore.IsTaken = true;
                }

                await _unitOfWork.CommitAsync();
                ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, key, packageResponse);

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
        public async Task<BaseResponseViewModel<dynamic>> ConfirmReadyToDelivery(string staffId, string timeSlotId, string stationId)
        {
            try
            {
                PackageStaffResponse packageStaff = new PackageStaffResponse();
                PackageShipperResponse packageShipper = new PackageShipperResponse();

                var staff = await _unitOfWork.Repository<Staff>().GetAll()
                                         .FirstOrDefaultAsync(x => x.Id == Guid.Parse(staffId));

                var timeSlot = await _unitOfWork.Repository<TimeSlot>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(timeSlotId));

                var station = await _unitOfWork.Repository<Station>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(stationId));

                var keyStaff = RedisDbEnum.Staff.GetDisplayName() + ":" + staff.Store.StoreName + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");
                var redisValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyStaff, null);
                packageStaff = JsonConvert.DeserializeObject<PackageStaffResponse>(redisValue);

                var packageStation = packageStaff.PackageStations.Where(x => x.StationId == Guid.Parse(stationId) && x.IsShipperAssign == false).FirstOrDefault();

                if (packageStation.TotalQuantity > packageStation.ReadyQuantity)
                {
                    var packageMissingProduct = new PackageStationResponse
                    {
                        StationId = packageStation.StationId,
                        StationName = packageStation.StationName,
                        TotalQuantity = packageStation.TotalQuantity - packageStation.ReadyQuantity,
                        ReadyQuantity = 0,
                        IsShipperAssign = false,
                        PackageStationDetails = new List<PackageDetailResponse>(),
                        ListPackageMissing = new List<PackageDetailResponse>(),
                        ListOrder = new HashSet<KeyValuePair<Guid, string>>()
                    };
                    packageMissingProduct.ListPackageMissing.AddRange(packageStation.ListPackageMissing);
                    packageStaff.PackageStations.Add(packageMissingProduct);
                    packageStation.TotalQuantity = packageStation.ReadyQuantity;
                    packageStation.ListPackageMissing.Clear();
                }

                packageStation.IsShipperAssign = true;

                var packageTotalOrder = packageStaff.ProductTotalDetails.SelectMany(x => x.ProductDetails).Where(x => x.IsFinishPrepare == true && x.IsAssignToShipper == false).ToList();
                packageTotalOrder.ForEach(x => x.IsAssignToShipper = true);

                var keyShipper = RedisDbEnum.Shipper.GetDisplayName() + ":" + station.Code + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");
                var redisShipperValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyShipper, null);
                if (redisShipperValue.HasValue == true)
                {
                    packageShipper = JsonConvert.DeserializeObject<PackageShipperResponse>(redisShipperValue);
                }
                else
                {
                    packageShipper = new PackageShipperResponse()
                    {
                        PackageStoreShipperResponses = new List<PackageStoreShipperResponse>(),
                        PackStationDetailGroupByBoxes = new List<PackStationDetailGroupByBox>()
                    };
                }

                #region PackageStoreShipperResponses
                if (packageShipper.PackageStoreShipperResponses.FirstOrDefault(x => x.StoreId == staff.StoreId && x.IsTaken == false) is null)
                {
                    packageShipper.PackageStoreShipperResponses.Add(new PackageStoreShipperResponse
                    {
                        StoreId = (Guid)staff.StoreId,
                        StoreName = staff.Store.StoreName,
                        IsTaken = false,
                        TotalQuantity = 0,
                        PackStationDetailGroupByProducts = new List<PackStationDetailGroupByProduct>(),
                        ListOrderId = new List<KeyValuePair<Guid, bool>>()
                    });
                }
                var packShipperStore = packageShipper.PackageStoreShipperResponses.FirstOrDefault(x => x.StoreId == staff.StoreId && x.IsTaken == false);

                foreach (var pack in packageStation.PackageStationDetails)
                {
                    packShipperStore.TotalQuantity += pack.Quantity;
                    packShipperStore.PackStationDetailGroupByProducts.Add(new PackStationDetailGroupByProduct()
                    {
                        ProductId = pack.ProductId,
                        ProductName = pack.ProductName,
                        TotalQuantity = pack.Quantity,
                    });
                }
                #endregion

                foreach (var order in packageStation.ListOrder)
                {
                    packShipperStore.ListOrderId.Add(new KeyValuePair<Guid, bool>(order.Key, false));

                    var keyOrder = RedisDbEnum.OrderOperation.GetDisplayName() + ":" + order.Value;

                    var orderValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyOrder, null);
                    PackageOrderResponse packageOrder = JsonConvert.DeserializeObject<PackageOrderResponse>(orderValue);

                    packageShipper.PackStationDetailGroupByBoxes.AddRange(packageOrder.PackageOrderBoxes.Select(x => new PackStationDetailGroupByBox()
                    {
                        BoxId = x.BoxId,
                        BoxCode = x.BoxCode,
                        ListProduct = x.PackageOrderDetailModels.Select(x => new PackageDetailResponse()
                        {
                            ProductId = x.ProductId,
                            ProductName = x.ProductName,
                            Quantity = x.Quantity
                        }).ToList()
                    }).ToList());
                }

                ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, keyShipper, packageShipper);
                ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, keyStaff, packageStaff);
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
        public async Task<BaseResponseViewModel<PackageStaffResponse>> GetPackage(string staffId, string timeSlotId)
        {
            try
            {
                PackageStaffResponse packageResponse = new PackageStaffResponse();
                var staff = await _unitOfWork.Repository<Staff>().GetAll()
                                         .FirstOrDefaultAsync(x => x.Id == Guid.Parse(staffId));

                var timeSlot = await _unitOfWork.Repository<TimeSlot>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(timeSlotId));

                var key = RedisDbEnum.Staff.GetDisplayName() + ":" + staff.Store.StoreName + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");

                var redisValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, key, null);
                if (redisValue.HasValue == true)
                {
                    packageResponse = JsonConvert.DeserializeObject<PackageStaffResponse>(redisValue);
                }

                return new BaseResponseViewModel<PackageStaffResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = packageResponse
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<BaseResponseViewModel<PackageShipperResponse>> GetPackageForShipper(string staffId, string timeSlotId)
        {
            try
            {
                PackageShipperResponse packageShipperResponse = new PackageShipperResponse();

                var staff = await _unitOfWork.Repository<Staff>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(staffId));

                var timeSlot = await _unitOfWork.Repository<TimeSlot>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(timeSlotId));

                var key = RedisDbEnum.Shipper.GetDisplayName() + ":" + staff.Station.Code + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");

                var redisShipperValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, key, null);

                if (redisShipperValue.HasValue == true)
                {
                    packageShipperResponse = JsonConvert.DeserializeObject<PackageShipperResponse>(redisShipperValue);
                }

                return new BaseResponseViewModel<PackageShipperResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = packageShipperResponse
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<BaseResponseViewModel<List<PackStationDetailGroupByBox>>> GetListBoxGroupByProduct(string staffId, string timeSlotId, string productId)
        {
            try
            {
                var result = new List<PackStationDetailGroupByBox>();
                PackageShipperResponse packageShipperResponse = new PackageShipperResponse();

                var staff = await _unitOfWork.Repository<Staff>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(staffId));

                var timeSlot = await _unitOfWork.Repository<TimeSlot>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(timeSlotId));

                var product = await _unitOfWork.Repository<ProductAttribute>().GetAll().FirstOrDefaultAsync(x => x.Id == Guid.Parse(productId));

                var key = RedisDbEnum.Shipper.GetDisplayName() + ":" + staff.Station.Code + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");

                var redisShipperValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, key, null);

                if (redisShipperValue.HasValue == true)
                {
                    packageShipperResponse = JsonConvert.DeserializeObject<PackageShipperResponse>(redisShipperValue);
                }
                var packProduct = packageShipperResponse.PackageStoreShipperResponses.FirstOrDefault(x => x.StoreId == product.Product.StoreId
                                                                                                        && x.IsTaken == true && x.IsInBox == false);
                var packBox = packageShipperResponse.PackStationDetailGroupByBoxes.Where(x => x.ListProduct.Any(x => x.ProductId == product.Id));

                foreach (var id in packProduct.ListOrderId)
                {
                    var listOrderBox = await _unitOfWork.Repository<OrderBox>().GetAll().Where(x => x.OrderId == id.Key).ToListAsync();
                    result = packBox.Where(x => listOrderBox.Select(x => x.BoxId).Contains(x.BoxId)).Select(x => new PackStationDetailGroupByBox
                    {
                        BoxId = x.BoxId,
                        BoxCode = x.BoxCode,
                        ListProduct = x.ListProduct.Where(y => y.ProductId == product.Id).ToList(),
                    }).ToList();
                }

                return new BaseResponseViewModel<List<PackStationDetailGroupByBox>>()
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
        public async Task<BaseResponseViewModel<List<PackageStationResponse>>> GetPackageGroupByStation(string staffId, string timeSlotId)
        {
            try
            {
                var result = new List<PackageStationResponse>();
                PackageStaffResponse packageResponse = new PackageStaffResponse();
                var staff = await _unitOfWork.Repository<Staff>().GetAll()
                                         .FirstOrDefaultAsync(x => x.Id == Guid.Parse(staffId));

                var timeSlot = await _unitOfWork.Repository<TimeSlot>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(timeSlotId));

                var key = RedisDbEnum.Staff.GetDisplayName() + ":" + staff.Store.StoreName + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");

                var redisValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, key, null);
                if (redisValue.HasValue == true)
                {
                    packageResponse = JsonConvert.DeserializeObject<PackageStaffResponse>(redisValue);
                    if (packageResponse.PackageStations is not null)
                    {
                        result.AddRange(packageResponse.PackageStations);
                    }
                }
                return new BaseResponseViewModel<List<PackageStationResponse>>()
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
        public async Task<BaseResponseViewModel<PackageStaffResponse>> UpdatePackage(string staffId, UpdateProductPackageRequest request)
        {
            try
            {
                PackageStaffResponse packageResponse = new PackageStaffResponse();
                var staff = await _unitOfWork.Repository<Staff>().GetAll()
                                         .FirstOrDefaultAsync(x => x.Id == Guid.Parse(staffId));

                var timeSlot = await _unitOfWork.Repository<TimeSlot>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(request.TimeSlotId));

                var store = await _unitOfWork.Repository<Store>().GetAll()
                         .FirstOrDefaultAsync(x => x.Id == Guid.Parse(request.StoreId));
                var key = RedisDbEnum.Staff.GetDisplayName() + ":" + store.StoreName + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");

                var redisValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, key, null);
                if (redisValue.HasValue == true)
                {
                    packageResponse = JsonConvert.DeserializeObject<PackageStaffResponse>(redisValue);
                }

                switch (request.Type)
                {
                    case PackageUpdateTypeEnum.Confirm:
                        foreach (var productRequest in request.ProductsUpdate)
                        {
                            //số lượng sẽ cập nhật tại pack station
                            var numberConfirmStation = 0;
                            //numberHasConfirm là số lượng confirm bao gồm đã confirm và sắp confirm
                            var numberHasConfirm = 0;

                            var productTotal = packageResponse.ProductTotalDetails.Find(x => x.ProductId == Guid.Parse(productRequest));

                            #region update pack staff                            
                            numberHasConfirm = productTotal.PendingQuantity + productTotal.WaitingQuantity;

                            //cập nhật lại tổng số lượng từng stage
                            packageResponse.TotalProductPending -= productTotal.PendingQuantity;
                            packageResponse.TotalProductReady += productTotal.PendingQuantity;

                            //cập nhật lại số lượng trong productTotal
                            productTotal.ReadyQuantity += productTotal.PendingQuantity;
                            #endregion

                            #region update pack order và update order trên db (nếu có)
                            //lấy các order id chưa xác nhận order by đặt sớm để lấy ra cập nhật
                            var listOrderInPack = productTotal.ProductDetails.Where(x => x.IsFinishPrepare == false).OrderBy(x => x.CheckInDate);
                            foreach (var order in listOrderInPack)
                            {
                                if (numberHasConfirm == 0) break;

                                var packStation = packageResponse.PackageStations.FirstOrDefault(x => x.StationId == order.StationId && x.IsShipperAssign == false);

                                //cập nhật trong pack staff trước
                                var keyOrder = RedisDbEnum.OrderOperation.GetDisplayName() + ":" + order.OrderCode;

                                var orderValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyOrder, null);
                                PackageOrderResponse packageOrder = JsonConvert.DeserializeObject<PackageOrderResponse>(orderValue);

                                if (numberHasConfirm >= order.QuantityOfProduct)
                                {
                                    numberConfirmStation = order.QuantityOfProduct;

                                    numberHasConfirm -= order.QuantityOfProduct;
                                    order.IsFinishPrepare = true;
                                    packageOrder.NumberHasConfirm += order.QuantityOfProduct;
                                }
                                else if (numberHasConfirm < order.QuantityOfProduct)
                                {
                                    numberConfirmStation = numberHasConfirm;
                                    packageOrder.NumberHasConfirm += numberHasConfirm;

                                    numberHasConfirm = 0;
                                }
                                ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, keyOrder, packageOrder);

                                //cập nhật lại trên db
                                if (packageOrder.TotalConfirm == packageOrder.NumberHasConfirm + packageOrder.NumberCannotConfirm)
                                {
                                    packStation.ListOrder.Add(new KeyValuePair<Guid, string>(order.OrderId, order.OrderCode));

                                    var orderDb = await _unitOfWork.Repository<Order>().GetAll().FirstOrDefaultAsync(x => x.Id == order.OrderId);
                                    orderDb.OrderStatus = (int)OrderStatusEnum.FinishPrepare;

                                    await _unitOfWork.Repository<Order>().UpdateDetached(orderDb);
                                }
                                #endregion

                                #region update pack station
                                packStation.ReadyQuantity += numberConfirmStation;
                                var readyPack = packStation.PackageStationDetails.FirstOrDefault(x => x.ProductId == Guid.Parse(productRequest));
                                var missingPack = packStation.ListPackageMissing.FirstOrDefault(x => x.ProductId == Guid.Parse(productRequest));

                                missingPack.Quantity -= (int)numberConfirmStation;
                                if (missingPack.Quantity == 0)
                                {
                                    packStation.ListPackageMissing.Remove(missingPack);
                                }
                                if (readyPack is null)
                                {
                                    readyPack = new PackageDetailResponse()
                                    {
                                        ProductId = missingPack.ProductId,
                                        ProductName = missingPack.ProductName,
                                        Quantity = numberConfirmStation,
                                        BoxProducts = new List<BoxProduct>()
                                    };
                                    packStation.PackageStationDetails.Add(readyPack);
                                }
                                else
                                {
                                    readyPack.Quantity += numberConfirmStation;
                                }
                                var findBox = packageOrder.PackageOrderBoxes.Where(x => x.PackageOrderDetailModels.Any(x => x.ProductId == Guid.Parse(productRequest))).ToList();
                                readyPack.BoxProducts.AddRange(findBox.Select(x => new BoxProduct()
                                {
                                    BoxId = x.BoxId,
                                    BoxCode = x.BoxCode
                                }).ToList());
                                #endregion
                            }
                            // những giá trị cập nhật sau cuối
                            productTotal.WaitingQuantity = numberHasConfirm;
                            productTotal.PendingQuantity = 0;
                        }
                        _unitOfWork.CommitAsync();
                        break;

                    case PackageUpdateTypeEnum.Error:
                        var item = request.ProductsUpdate.FirstOrDefault();
                        var product = packageResponse.ProductTotalDetails.FirstOrDefault(x => x.ProductId == Guid.Parse(item));

                        packageResponse.TotalProductError += (int)request.Quantity;
                        product.ErrorQuantity += (int)request.Quantity;

                        //cập nhật số errorProduct trong order pack
                        var errorNum = request.Quantity;
                        var orderList = product.ProductDetails.Where(x => x.IsFinishPrepare == false).OrderByDescending(x => x.CheckInDate);

                        foreach (var order in orderList)
                        {
                            if (errorNum == 0) break;
                            if (errorNum > order.QuantityOfProduct)
                            {
                                product.ErrorQuantity = order.QuantityOfProduct;
                                errorNum -= order.QuantityOfProduct;
                            }
                            else
                            {
                                order.ErrorQuantity = (int)errorNum;
                                errorNum = 0;
                            }
                        }
                        //cập nhật số errorProduct trong staff pack
                        if (packageResponse.ErrorProducts is null)
                        {
                            packageResponse.ErrorProducts = new List<ErrorProduct>();
                        }
                        switch (staff.RoleType)
                        {
                            case (int)SystemRoleTypeEnum.StoreManager:
                                packageResponse.TotalProductPending -= (int)request.Quantity;
                                product.PendingQuantity -= (int)request.Quantity;

                                if (packageResponse.ErrorProducts.Any(x => x.ProductId == Guid.Parse(item)
                                                                        && x.ReportMemType == (int)SystemRoleTypeEnum.StoreManager && x.IsRefuse == false) is true)
                                {
                                    packageResponse.ErrorProducts.Find(x => x.ProductId == Guid.Parse(item) && x.ReportMemType == (int)SystemRoleTypeEnum.StoreManager).Quantity += (int)request.Quantity;
                                }
                                else
                                {
                                    packageResponse.ErrorProducts.Add(new ErrorProduct()
                                    {
                                        ProductId = product.ProductId,
                                        ProductInMenuId = product.ProductInMenuId,
                                        ProductName = product.ProductName,
                                        Quantity = (int)request.Quantity,
                                        ReConfirmQuantity = 0,
                                        ReportMemType = (int)SystemRoleTypeEnum.StoreManager,
                                        IsRefuse = false
                                    });
                                }
                                break;

                            case (int)SystemRoleTypeEnum.Shipper:
                                var keyShipper = RedisDbEnum.Shipper.GetDisplayName() + ":" + staff.Station.Code + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");

                                var redisShipperValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, key, null);

                                var packageShipperResponse = JsonConvert.DeserializeObject<PackageShipperResponse>(redisShipperValue);

                                var productPackshipper = packageShipperResponse.PackageStoreShipperResponses.Where(x => x.StoreId == store.Id
                                                                                            && x.IsTaken == true)
                                                                                    .Select(x => x.PackStationDetailGroupByProducts.FirstOrDefault(x => x.ProductId == product.ProductId))
                                                                                    .FirstOrDefault();
                                productPackshipper.ErrorQuantity += (int)request.Quantity;
                                if (packageResponse.ErrorProducts.Any(x => x.ProductId == Guid.Parse(item)
                                                                        && x.ReportMemType == (int)SystemRoleTypeEnum.Shipper && x.IsRefuse == false) is true)
                                {
                                    var errorPack = packageResponse.ErrorProducts.Find(x => x.ProductId == Guid.Parse(item) && x.ReportMemType == (int)SystemRoleTypeEnum.Shipper && x.IsRefuse == false);
                                    errorPack.Quantity += (int)request.Quantity;
                                    errorPack.ListBox.Add((Guid)request.BoxId);
                                }
                                else
                                {
                                    packageResponse.ErrorProducts.Add(new ErrorProduct()
                                    {
                                        ProductId = product.ProductId,
                                        ProductInMenuId = product.ProductInMenuId,
                                        ProductName = product.ProductName,
                                        Quantity = (int)request.Quantity,
                                        StationId = staff.StationId,
                                        ReConfirmQuantity = 0,
                                        ReportMemType = (int)SystemRoleTypeEnum.Shipper,
                                        ListBox = new List<Guid>()
                                        {
                                            (Guid)request.BoxId
                                        },
                                        IsRefuse = false
                                    });
                                }
                                break;
                        }
                        break;

                    case PackageUpdateTypeEnum.ReConfirm:
                        //số lượng confirm bao gồm đã confirm còn dư và sắp confirm
                        var numberOfConfirm = 0;
                        //số lượng sẽ cập nhật tại pack station
                        var numberUpdateAtStation = 0;

                        var productRequestId = request.ProductsUpdate.FirstOrDefault();
                        var productTotalPack = packageResponse.ProductTotalDetails.FirstOrDefault(x => x.ProductId == Guid.Parse(productRequestId));

                        numberOfConfirm = (int)request.Quantity + productTotalPack.WaitingQuantity;

                        //cập nhật lại tổng số lượng từng stage
                        packageResponse.TotalProductError -= (int)request.Quantity;
                        packageResponse.TotalProductReady += (int)request.Quantity;

                        //cập nhật lại số lượng trong productTotal
                        productTotalPack.ReadyQuantity += (int)request.Quantity;
                        productTotalPack.ErrorQuantity -= (int)request.Quantity;

                        //cập nhật error pack
                        var packageError = packageResponse.ErrorProducts.Where(x => x.ProductId == Guid.Parse(productRequestId)).FirstOrDefault();
                        if (request.Quantity + packageError.ReConfirmQuantity == packageError.Quantity)
                        {
                            packageResponse.ErrorProducts.Remove(packageError);
                        }
                        else
                        {
                            packageError.ReConfirmQuantity += (int)request.Quantity;
                        }
                        #region update pack order và update order trên db (nếu có)
                        //lấy các order id chưa xác nhận order by đặt sớm để lấy ra cập nhật
                        var listOrder = productTotalPack.ProductDetails.Where(x => x.IsFinishPrepare == false).OrderByDescending(x => x.CheckInDate);
                        foreach (var order in listOrder)
                        {
                            if (numberOfConfirm == 0) break;

                            var keyOrder = RedisDbEnum.OrderOperation.GetDisplayName() + ":" + order.OrderCode;
                            var orderValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyOrder, null);
                            PackageOrderResponse packageOrder = JsonConvert.DeserializeObject<PackageOrderResponse>(orderValue);

                            var stationPack = packageResponse.PackageStations.FirstOrDefault(x => x.StationId == order.StationId && x.IsShipperAssign == false);

                            if (request.Quantity >= order.ErrorQuantity)
                            {
                                order.IsFinishPrepare = true;
                                packageOrder.NumberHasConfirm += order.ErrorQuantity;

                                numberOfConfirm -= order.ErrorQuantity;
                                numberUpdateAtStation = order.ErrorQuantity;
                                request.Quantity -= order.ErrorQuantity;
                            }
                            else if (request.Quantity < order.ErrorQuantity)
                            {
                                order.ErrorQuantity -= (int)request.Quantity;
                                numberUpdateAtStation = (int)request.Quantity;
                                packageOrder.NumberHasConfirm += (int)request.Quantity;

                                numberOfConfirm = 0;
                            }

                            if (packageOrder.TotalConfirm == packageOrder.NumberHasConfirm + packageOrder.NumberCannotConfirm)
                            {
                                stationPack.ListOrder.Add(new KeyValuePair<Guid, string>(order.OrderId, order.OrderCode));
                                var orderDb = await _unitOfWork.Repository<Order>().GetAll().FirstOrDefaultAsync(x => x.Id == order.OrderId);
                                orderDb.OrderStatus = (int)OrderStatusEnum.FinishPrepare;

                                await _unitOfWork.Repository<Order>().UpdateDetached(orderDb);
                            }
                            ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, keyOrder, packageOrder);
                            #endregion

                            #region update pack station
                            stationPack.ReadyQuantity += numberUpdateAtStation;
                            var readyPack = stationPack.PackageStationDetails.FirstOrDefault(x => x.ProductId == Guid.Parse(productRequestId));
                            var missingPack = stationPack.ListPackageMissing.FirstOrDefault(x => x.ProductId == Guid.Parse(productRequestId));

                            missingPack.Quantity -= (int)numberUpdateAtStation;
                            if (missingPack.Quantity == 0)
                            {
                                stationPack.ListPackageMissing.Remove(missingPack);
                            }

                            if (readyPack is null)
                            {
                                readyPack = new PackageDetailResponse()
                                {
                                    ProductId = missingPack.ProductId,
                                    ProductName = missingPack.ProductName,
                                    Quantity = numberUpdateAtStation
                                };
                                stationPack.PackageStationDetails.Add(readyPack);
                            }
                            else
                            {
                                readyPack.Quantity += numberUpdateAtStation;
                            }
                            var findBox = packageOrder.PackageOrderBoxes.Where(x => x.PackageOrderDetailModels.Any(x => x.ProductId == Guid.Parse(productRequestId))).ToList();
                            readyPack.BoxProducts.AddRange(findBox.Select(x => new BoxProduct()
                            {
                                BoxId = x.BoxId,
                                BoxCode = x.BoxCode
                            }).ToList());
                            #endregion
                        }
                        productTotalPack.WaitingQuantity = numberOfConfirm;
                        await _unitOfWork.CommitAsync();
                        break;
                }
                ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, key, packageResponse);
                return new BaseResponseViewModel<PackageStaffResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = packageResponse
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<BaseResponseViewModel<dynamic>> ConfirmAllInBox(string staffId, string timeSlotId)
        {
            try
            {
                PackageShipperResponse packageShipperResponse = new PackageShipperResponse();

                var staff = await _unitOfWork.Repository<Staff>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(staffId));

                var timeSlot = await _unitOfWork.Repository<TimeSlot>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(timeSlotId));

                var key = RedisDbEnum.Shipper.GetDisplayName() + ":" + staff.Station.Code + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");

                var redisShipperValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, key, null);

                if (redisShipperValue.HasValue == true)
                {
                    packageShipperResponse = JsonConvert.DeserializeObject<PackageShipperResponse>(redisShipperValue);
                }
                HashSet<Guid> listOrder = new HashSet<Guid>();
                var packStore = packageShipperResponse.PackageStoreShipperResponses.Where(x => x.IsTaken == true && x.IsInBox == false);
                listOrder = listOrder.Concat(packStore.SelectMany(x => x.ListOrderId).Select(x => x.Key)).ToHashSet();
                packStore.Select(x => x.IsInBox = true);

                foreach (var order in packStore)
                {
                    order.IsInBox = true;
                }
                foreach (var orderId in listOrder)
                {
                    var order = _unitOfWork.Repository<Order>().GetAll().FirstOrDefault(x => x.Id == orderId);
                    order.OrderStatus = (int)OrderStatusEnum.BoxStored;
                    _unitOfWork.Repository<Order>().UpdateDetached(order);
                }

                _unitOfWork.Commit();
                ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, key, packageShipperResponse);

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
    }
}
