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
                List<PackageShipperResponse> packageResponse = new List<PackageShipperResponse>();

                var staff = await _unitOfWork.Repository<Staff>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(staffId));

                var timeSlot = await _unitOfWork.Repository<TimeSlot>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(timeSlotId));

                var key = RedisDbEnum.Shipper.GetDisplayName() + ":" + staff.Station.Code + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");

                var redisShipperValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, key, null);

                if (redisShipperValue.HasValue == true)
                {
                    packageResponse = JsonConvert.DeserializeObject<List<PackageShipperResponse>>(redisShipperValue);
                }
                packageResponse.FirstOrDefault(x => x.StoreId == Guid.Parse(storeId) && x.IsTaken == false).IsTaken = true;

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
                var staff = await _unitOfWork.Repository<Staff>().GetAll()
                                         .FirstOrDefaultAsync(x => x.Id == Guid.Parse(staffId));

                var timeSlot = await _unitOfWork.Repository<TimeSlot>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(timeSlotId));

                var station = await _unitOfWork.Repository<Station>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(stationId));

                var keyStaff = RedisDbEnum.Staff.GetDisplayName() + ":" + staff.Store.StoreName + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");
                var redisValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyStaff, null);
                packageResponse = JsonConvert.DeserializeObject<PackageResponse>(redisValue);

                var packageStation = packageResponse.PackageStations.Where(x => x.StationId == Guid.Parse(stationId) && x.IsShipperAssign == false).FirstOrDefault();
                packageStation.IsShipperAssign = true;

                var packageOrder = packageResponse.ProductTotalDetails.SelectMany(x => x.ProductDetails).Where(x => x.IsFinishPrepare == true && x.IsAssignToShipper == false).ToList();
                packageOrder.ForEach(x => x.IsAssignToShipper = true);

                var keyShipper = RedisDbEnum.Shipper.GetDisplayName() + ":" + station.Code + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");
                var redisShipperValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyShipper, null);

                List<PackageShipperResponse> packageShipper = new List<PackageShipperResponse>();

                if (redisShipperValue.HasValue == true)
                {
                    packageShipper = JsonConvert.DeserializeObject<List<PackageShipperResponse>>(redisValue);
                }
                if (redisShipperValue.HasValue == false || packageShipper.FirstOrDefault(x => x.StoreId == staff.StoreId && x.IsTaken == false) is null)
                {
                    packageShipper.Add(new PackageShipperResponse()
                    {
                        StoreId = (Guid)staff.StoreId,
                        StoreName = staff.Store.StoreName,
                        IsTaken = false,
                        PackageShipperDetails = new List<PackageDetailResponse>()
                    });
                }
                foreach (var pack in packageStation.PackageStationDetails)
                {
                    packageShipper.FirstOrDefault(x => x.StoreId == staff.StoreId && x.IsTaken == false).PackageShipperDetails.Add(new PackageDetailResponse()
                    {
                        ProductId = pack.ProductId,
                        ProductName = pack.ProductName,
                        Quantity = pack.Quantity,
                    });
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
                List<PackageShipperResponse> packageResponse = new List<PackageShipperResponse>();

                var staff = await _unitOfWork.Repository<Staff>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(staffId));

                var timeSlot = await _unitOfWork.Repository<TimeSlot>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(timeSlotId));

                var key = RedisDbEnum.Shipper.GetDisplayName() + ":" + staff.Station.Code + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");

                var redisShipperValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, key, null);

                if (redisShipperValue.HasValue == true)
                {
                    packageResponse = JsonConvert.DeserializeObject<List<PackageShipperResponse>>(redisShipperValue);
                }

                return new BaseResponseViewModel<List<PackageShipperResponse>>()
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
                        foreach (var item in request.ProductsUpdate)
                        {
                            //số lượng sẽ cập nhật tại pack station
                            var numberConfirmStation = 0;
                            //numberHasConfirm là số lượng confirm bao gồm đã confirm và sắp confirm
                            var numberHasConfirm = 0;

                            var productTotal = packageResponse.ProductTotalDetails.Find(x => x.ProductId == Guid.Parse(item));

                            #region update pack staff                            
                            numberHasConfirm = productTotal.PendingQuantity + productTotal.WaitingQuantity;

                            //cập nhật lại tổng số lượng từng stage
                            packageResponse.TotalProductPending -= productTotal.PendingQuantity;
                            packageResponse.TotalProductReady += productTotal.PendingQuantity;

                            //cập nhật lại số lượng trong productTotal
                            productTotal.ReadyQuantity += productTotal.PendingQuantity;
                            productTotal.PendingQuantity = 0;
                            productTotal.WaitingQuantity = numberHasConfirm;

                            //cập nhật lại productTotal.ReadyQuantity += productTotal.PendingQuantity;
                            #endregion

                            #region update pack order và update order trên db (nếu có)
                            //lấy các order id chưa xác nhận order by đặt sớm để lấy ra cập nhật
                            var listOrder = productTotal.ProductDetails.Where(x => x.IsFinishPrepare == false).OrderBy(x => x.CheckInDate);
                            foreach (var order in listOrder)
                            {
                                //cập nhật trong pack staff trước


                                var keyOrder = RedisDbEnum.OrderOperation.GetDisplayName() + ":" + order.OrderCode;
                                var orderValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyOrder, null);
                                List<PackageOrderDetailModel> packageOrderDetail = JsonConvert.DeserializeObject<List<PackageOrderDetailModel>>(orderValue);

                                var productInOrder = packageOrderDetail.FirstOrDefault(x => x.ProductId == Guid.Parse(item));
                                if (numberHasConfirm >= productInOrder.Quantity)
                                {
                                    numberConfirmStation = productInOrder.Quantity;

                                    numberHasConfirm -= productInOrder.Quantity;
                                    productInOrder.IsReady = true;
                                    order.IsFinishPrepare = true;
                                }
                                else
                                {
                                    numberConfirmStation = productTotal.PendingQuantity;
                                }
                                ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, keyOrder, packageOrderDetail);
                                //cập nhật lại trên db
                                if (packageOrderDetail.All(x => x.IsReady) is true)
                                {
                                    var orderDb = await _unitOfWork.Repository<Order>().GetAll().FirstOrDefaultAsync(x => x.Id == order.OrderId);
                                    orderDb.OrderStatus = (int)OrderStatusEnum.FinishPrepare;

                                    await _unitOfWork.Repository<Order>().UpdateDetached(orderDb);
                                }
                                #endregion

                                #region update pack station
                                var packStation = packageResponse.PackageStations.FirstOrDefault(x => x.StationId == order.StationId);

                                packStation.ReadyQuantity += numberConfirmStation;
                                var readyPack = packStation.PackageStationDetails.FirstOrDefault(x => x.ProductId == Guid.Parse(item));
                                var missingPack = packStation.ListPackageMissing.FirstOrDefault(x => x.ProductId == Guid.Parse(item));

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
                            }
                            #endregion
                        }
                        _unitOfWork.CommitAsync();
                        break;

                    case PackageUpdateTypeEnum.Error:
                        switch (staff.RoleType)
                        {
                            case (int)SystemRoleTypeEnum.StoreManager:
                                foreach (var item in request.ProductsUpdate)
                                {
                                    var product = packageResponse.ProductTotalDetails.Find(x => x.ProductId == Guid.Parse(item));

                                    packageResponse.TotalProductError += (int)request.Quantity;
                                    packageResponse.TotalProductPending -= (int)request.Quantity;

                                    product.PendingQuantity -= (int)request.Quantity;
                                    product.ErrorQuantity += (int)request.Quantity;

                                    if (packageResponse.ErrorProducts is not null && packageResponse.ErrorProducts.Any(x => x.ProductId == Guid.Parse(item)
                                                                                                    && x.ReportMemType == (int)SystemRoleTypeEnum.StoreManager) is true)
                                    {
                                        packageResponse.ErrorProducts.Find(x => x.ProductId == Guid.Parse(item) && x.ReportMemType == (int)SystemRoleTypeEnum.StoreManager).Quantity += (int)request.Quantity;
                                    }
                                    else
                                    {
                                        packageResponse.ErrorProducts = new List<ErrorProduct>();
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
                                }
                                break;

                            case (int)SystemRoleTypeEnum.Shipper:
                                foreach (var item in request.ProductsUpdate)
                                {
                                    var product = packageResponse.ProductTotalDetails.Find(x => x.ProductId == Guid.Parse(item));

                                    packageResponse.TotalProductError += (int)request.Quantity;
                                    product.ErrorQuantity += (int)request.Quantity;

                                    if (packageResponse.ErrorProducts is not null && packageResponse.ErrorProducts.Any(x => x.ProductId == Guid.Parse(item)
                                                                                                   && x.ReportMemType == (int)SystemRoleTypeEnum.Shipper) is true)
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
                                }
                                break;
                        }
                        break;

                    case PackageUpdateTypeEnum.ReConfirm:
                        foreach (var item in request.ProductsUpdate)
                        {
                            //cập nhật error pack
                            var packageError = packageResponse.ErrorProducts.Where(x => x.ProductId == Guid.Parse(item)).FirstOrDefault();
                            if (request.Quantity + packageError.ReConfirmQuantity == packageError.Quantity)
                            {
                                packageResponse.ErrorProducts.Remove(packageError);
                            }
                            else
                            {
                                packageError.ReConfirmQuantity += (int)request.Quantity;
                            }

                            var product = packageResponse.ProductTotalDetails.Find(x => x.ProductId == Guid.Parse(item));

                            var numberOfConfirm = request.Quantity + product.WaitingQuantity;
                            packageResponse.TotalProductError -= (int)request.Quantity;
                            packageResponse.TotalProductReady += (int)request.Quantity;

                            product.ReadyQuantity += (int)request.Quantity;
                            product.ErrorQuantity -= (int)request.Quantity;

                            //cập nhật lại order
                            var listOrder = product.ProductDetails.Where(x => x.IsFinishPrepare == false).OrderByDescending(x => x.CheckInDate);

                            var numberConfirmRequest = request.Quantity;
                            foreach (var order in listOrder)
                            {
                                var numberUpdateAtStation = 0;
                                if (numberConfirmRequest == 0) break;

                                var keyOrder = RedisDbEnum.OrderOperation.GetDisplayName() + ":" + order.OrderCode;
                                var orderValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyOrder, null);
                                List<PackageOrderDetailModel> packageOrderDetail = JsonConvert.DeserializeObject<List<PackageOrderDetailModel>>(orderValue);

                                var productInOrder = packageOrderDetail.FirstOrDefault(x => x.ProductId == Guid.Parse(item));
                                productInOrder.ErrorQuantity -= (int)numberConfirmRequest;

                                if (order.Quantity <= numberOfConfirm)
                                {
                                    order.IsFinishPrepare = true;
                                    productInOrder.IsReady = true;
                                    numberUpdateAtStation = order.Quantity;
                                    numberConfirmRequest -= order.Quantity;
                                }
                                else if (order.Quantity > numberConfirmRequest)
                                {
                                    numberUpdateAtStation = (int)numberConfirmRequest;
                                    numberConfirmRequest = 0;
                                }

                                if (packageOrderDetail.All(x => x.IsReady) is true)
                                {
                                    var orderDb = await _unitOfWork.Repository<Order>().GetAll().FirstOrDefaultAsync(x => x.Id == order.OrderId);
                                    orderDb.OrderStatus = (int)OrderStatusEnum.FinishPrepare;

                                    await _unitOfWork.Repository<Order>().UpdateDetached(orderDb);
                                }

                                //cập nhật lại pack station
                                var stationPack = packageResponse.PackageStations.FirstOrDefault(x => x.StationId == order.StationId);
                                stationPack.ReadyQuantity += numberUpdateAtStation;

                                var missingPack = stationPack.ListPackageMissing.FirstOrDefault(x => x.ProductId == Guid.Parse(item));
                                missingPack.Quantity -= (int)numberUpdateAtStation;
                                if (missingPack.Quantity == 0)
                                {
                                    stationPack.ListPackageMissing.Remove(missingPack);
                                }

                                var readyPack = stationPack.PackageStationDetails.FirstOrDefault(x => x.ProductId == Guid.Parse(item));
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
                            }
                        }
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
    }
}
