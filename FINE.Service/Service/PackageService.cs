using AutoMapper;
using Azure.Core;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.DTO.Request.Package;
using FINE.Service.DTO.Response;
using FINE.Service.Helpers;
using FINE.Service.Utilities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;

namespace FINE.Service.Service
{
    public interface IPackageService
    {
        Task<BaseResponseViewModel<PackageResponse>> GetPackage(string staffId, string timeSlotId);
        Task<BaseResponseViewModel<List<PackageStationResponse>>> GetPackageGroupByStation(string staffId, string timeSlotId);
        Task<BaseResponseViewModel<List<PackageShipperResponse>>> GetPackageForShipper(string staffId, string timeSlotId);
        Task<BaseResponseViewModel<PackageResponse>> UpdatePackage(string staffId, UpdateProductPackageRequest request);
        Task<BaseResponseViewModel<PackageResponse>> ConfirmReadyToDelivery(string staffId, string timeSlotId, string stationId);
        Task<BaseResponseViewModel<List<PackageShipperResponse>>> ConfirmTakenPackage(string staffId, string timeSlotId, string storeId);
        Task<BaseResponseViewModel<dynamic>> ConfirmAllInBox(string staffId, string timeSlotId);
    }
    public class PackageService : IPackageService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public PackageService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponseViewModel<List<PackageShipperResponse>>> ConfirmTakenPackage(string staffId, string timeSlotId, string storeId)
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
                    var orderDb = await _unitOfWork.Repository<Order>().GetAll().FirstOrDefaultAsync(x => x.Id == order);
                    orderDb.OrderStatus = (int)OrderStatusEnum.Delivering;

                    await _unitOfWork.Repository<Order>().UpdateDetached(orderDb);
                }
                packStore.IsTaken = true;
                await _unitOfWork.CommitAsync();
                ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, key, packageResponse);

                return new BaseResponseViewModel<List<PackageShipperResponse>>()
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
        public async Task<BaseResponseViewModel<PackageResponse>> ConfirmReadyToDelivery(string staffId, string timeSlotId, string stationId)
        {
            try
            {
                PackageResponse packageResponse = new PackageResponse();
                PackageShipperResponse packageShipper = new PackageShipperResponse();

                var staff = await _unitOfWork.Repository<Staff>().GetAll()
                                         .FirstOrDefaultAsync(x => x.Id == Guid.Parse(staffId));

                var timeSlot = await _unitOfWork.Repository<TimeSlot>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(timeSlotId));

                var station = await _unitOfWork.Repository<Station>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(stationId));

                var keyStaff = RedisDbEnum.Staff.GetDisplayName() + ":" + staff.Store.StoreName + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");
                var redisValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyStaff, null);

                packageResponse = JsonConvert.DeserializeObject<PackageResponse>(redisValue);

                var keyShipper = RedisDbEnum.Shipper.GetDisplayName() + ":" + station.Code + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");
                var redisShipperValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyShipper, null);

                var packageStation = packageResponse.PackageStations.Where(x => x.StationId == Guid.Parse(stationId) && x.IsShipperAssign == false).FirstOrDefault();
                packageStation.IsShipperAssign = true;

                var packageTotalOrder = packageResponse.ProductTotalDetails.SelectMany(x => x.ProductDetails).Where(x => x.IsFinishPrepare == true && x.IsAssignToShipper == false).ToList();
                packageTotalOrder.ForEach(x => x.IsAssignToShipper = true);

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
                        ListOrderId = new List<Guid>()
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
                        BoxCode = new HashSet<Guid>()
                    });
                }
                #endregion

                foreach(var order in packageStation.ListOrder)
                {
                    var keyOrder = RedisDbEnum.OrderOperation.GetDisplayName() + ":" + order.Value;

                    var orderValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyOrder, null);
                    PackageOrderModel packageOrder = JsonConvert.DeserializeObject<PackageOrderModel>(orderValue);

                    packageShipper.PackStationDetailGroupByBoxes =  packageOrder.PackageOrderBoxes.Select(x => new PackStationDetailGroupByBox()
                    {
                        BoxId = x.BoxId,
                        BoxCode = x.BoxCode,
                        IsInBox = false,
                        ListProduct = x.PackageOrderDetailModels.Select(x => new PackageDetailResponse()
                        {
                            ProductId = x.ProductId,
                            ProductName = x.ProductName,
                            Quantity = x.Quantity
                        }).ToList()
                    }).ToList();
                }

                ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, keyShipper, packageShipper);
                ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, keyStaff, packageResponse);
                return new BaseResponseViewModel<PackageResponse>()
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
        public async Task<BaseResponseViewModel<PackageResponse>> GetPackage(string staffId, string timeSlotId)
        {
            try
            {
                PackageResponse packageResponse = new PackageResponse();
                var staff = await _unitOfWork.Repository<Staff>().GetAll()
                                         .FirstOrDefaultAsync(x => x.Id == Guid.Parse(staffId));

                var timeSlot = await _unitOfWork.Repository<TimeSlot>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(timeSlotId));

                var key = RedisDbEnum.Staff.GetDisplayName() + ":" + staff.Store.StoreName + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");

                var redisValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, key, null);
                if (redisValue.HasValue == true)
                {
                    packageResponse = JsonConvert.DeserializeObject<PackageResponse>(redisValue);
                }

                return new BaseResponseViewModel<PackageResponse>()
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
        public async Task<BaseResponseViewModel<List<PackageShipperResponse>>> GetPackageForShipper(string staffId, string timeSlotId)
        {
            try
            {
                List<PackageShipperResponse> packageShipperResponse = new List<PackageShipperResponse>();

                var staff = await _unitOfWork.Repository<Staff>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(staffId));

                var timeSlot = await _unitOfWork.Repository<TimeSlot>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(timeSlotId));

                var key = RedisDbEnum.Shipper.GetDisplayName() + ":" + staff.Station.Code + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");

                var redisShipperValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, key, null);

                if (redisShipperValue.HasValue == true)
                {
                    packageShipperResponse = JsonConvert.DeserializeObject<List<PackageShipperResponse>>(redisShipperValue);
                }

                return new BaseResponseViewModel<List<PackageShipperResponse>>()
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
        public async Task<BaseResponseViewModel<List<PackageStationResponse>>> GetPackageGroupByStation(string staffId, string timeSlotId)
        {
            try
            {
                var result = new List<PackageStationResponse>();
                PackageResponse packageResponse = new PackageResponse();
                var staff = await _unitOfWork.Repository<Staff>().GetAll()
                                         .FirstOrDefaultAsync(x => x.Id == Guid.Parse(staffId));

                var timeSlot = await _unitOfWork.Repository<TimeSlot>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(timeSlotId));

                var key = RedisDbEnum.Staff.GetDisplayName() + ":" + staff.Store.StoreName + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");

                var redisValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, key, null);
                if (redisValue.HasValue == true)
                {
                    packageResponse = JsonConvert.DeserializeObject<PackageResponse>(redisValue);
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
        public async Task<BaseResponseViewModel<PackageResponse>> UpdatePackage(string staffId, UpdateProductPackageRequest request)
        {
            try
            {
                PackageResponse packageResponse = new PackageResponse();
                var staff = await _unitOfWork.Repository<Staff>().GetAll()
                                         .FirstOrDefaultAsync(x => x.Id == Guid.Parse(staffId));

                var timeSlot = await _unitOfWork.Repository<TimeSlot>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(request.timeSlotId));

                var key = RedisDbEnum.Staff.GetDisplayName() + ":" + staff.Store.StoreName + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");

                var redisValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, key, null);
                if (redisValue.HasValue == true)
                {
                    packageResponse = JsonConvert.DeserializeObject<PackageResponse>(redisValue);
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

                                var packStation = packageResponse.PackageStations.FirstOrDefault(x => x.StationId == order.StationId);

                                //cập nhật trong pack staff trước
                                var keyOrder = RedisDbEnum.OrderOperation.GetDisplayName() + ":" + order.OrderCode;

                                var orderValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyOrder, null);
                                PackageOrderModel packageOrder = JsonConvert.DeserializeObject<PackageOrderModel>(orderValue);

                                if (numberHasConfirm >= order.QuantityOfProduct)
                                {
                                    numberConfirmStation = order.QuantityOfProduct;

                                    numberHasConfirm -= order.QuantityOfProduct;
                                    order.IsFinishPrepare = true;
                                    packageOrder.NumberHasConfirm += 1;
                                }
                                else if (numberHasConfirm < order.QuantityOfProduct)
                                {
                                    numberConfirmStation = numberHasConfirm;
                                    numberHasConfirm = 0;
                                }
                                ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, keyOrder, packageOrder);

                                //cập nhật lại trên db
                                if (packageOrder.TotalConfirm == packageOrder.NumberHasConfirm)
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
                                        Quantity = numberConfirmStation
                                    };
                                    packStation.PackageStationDetails.Add(readyPack);
                                }
                                else
                                {
                                    readyPack.Quantity += numberConfirmStation;
                                }
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
                        var orderList = packageResponse.ProductTotalDetails.SelectMany(x => x.ProductDetails).Where(x => x.IsFinishPrepare == false).OrderByDescending(x => x.CheckInDate);

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
                                                                                            && x.ReportMemType == (int)SystemRoleTypeEnum.StoreManager) is true)
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
                                    });
                                }
                                break;

                            case (int)SystemRoleTypeEnum.Shipper:
                                if (packageResponse.ErrorProducts.Any(x => x.ProductId == Guid.Parse(item) && x.ReportMemType == (int)SystemRoleTypeEnum.Shipper) is true)
                                {
                                    packageResponse.ErrorProducts.Find(x => x.ProductId == Guid.Parse(item) && x.ReportMemType == (int)SystemRoleTypeEnum.Shipper).Quantity += (int)request.Quantity;
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
                            PackageOrderModel packageOrder = JsonConvert.DeserializeObject<PackageOrderModel>(orderValue);
                            packageOrder.NumberHasConfirm += 1;

                            var stationPack = packageResponse.PackageStations.FirstOrDefault(x => x.StationId == order.StationId);

                            if (request.Quantity >= order.ErrorQuantity)
                            {
                                order.IsFinishPrepare = true;

                                numberOfConfirm -= order.ErrorQuantity;
                                numberUpdateAtStation = order.ErrorQuantity;
                                request.Quantity -= order.ErrorQuantity;
                            }
                            else if (request.Quantity < order.ErrorQuantity)
                            {
                                order.ErrorQuantity -= (int)request.Quantity;
                                numberUpdateAtStation = (int)request.Quantity;
                                numberOfConfirm = 0;
                            }

                            if (packageOrder.TotalConfirm == packageOrder.NumberHasConfirm)
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
                            #endregion
                        }
                        productTotalPack.WaitingQuantity = numberOfConfirm;
                        await _unitOfWork.CommitAsync();
                        break;
                }
                ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, key, packageResponse);
                return new BaseResponseViewModel<PackageResponse>()
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
                foreach (var pack in packageShipperResponse.PackStationDetailGroupByBoxes)
                {
                    pack.IsInBox = true;
                }
                await ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, key, packageShipperResponse);
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
