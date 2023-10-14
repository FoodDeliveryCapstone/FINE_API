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

                var packageStation = packageResponse.PackageStations.Where(x => x.IsShipperAssign == false).FirstOrDefault();
                packageStation.IsShipperAssign = true;

                var keyShipper = RedisDbEnum.Shipper.GetDisplayName() + station.Code + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");
                var redisShipperValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyShipper, null);

                PackageShipperResponse packageShipper = new PackageShipperResponse();

                if (redisShipperValue.HasValue == true)
                {
                    packageShipper = JsonConvert.DeserializeObject<PackageShipperResponse>(redisValue);
                }
                else
                {
                    packageShipper = new PackageShipperResponse()
                    {
                        StoreId = (Guid)staff.StoreId,
                        StoreName = staff.Store.StoreName,
                        PackageShipperDetails = new List<PackageDetailResponse>()
                    };
                }
                foreach (var pack in packageStation.PackageStationDetails)
                {
                    packageShipper.PackageShipperDetails.Add(new PackageDetailResponse()
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
                            var product = packageResponse.ProductTotalDetails.Find(x => x.ProductId == Guid.Parse(item));
                            var numberOfConfirm = product.PendingQuantity + product.WaitingQuantity;

                            packageResponse.TotalProductPending -= product.PendingQuantity;
                            packageResponse.TotalProductReady += product.PendingQuantity;

                            product.ReadyQuantity += product.PendingQuantity;
                            product.PendingQuantity = 0;

                            var listOrder = product.ProductDetails.Where(x => x.IsReady == false).OrderBy(x => x.CheckInDate);

                            foreach (var order in listOrder)
                            {
                                var keyOrder = RedisDbEnum.OrderOperation.GetDisplayName() + ":" + order.OrderCode;
                                var orderValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyOrder, null);
                                List<PackageOrderDetailModel> packageOrderDetail = JsonConvert.DeserializeObject<List<PackageOrderDetailModel>>(orderValue);

                                var productInOrder = packageOrderDetail.FirstOrDefault(x => x.ProductId == Guid.Parse(item));
                                if (numberOfConfirm >= productInOrder.Quantity)
                                {
                                    numberOfConfirm -= productInOrder.Quantity;
                                    productInOrder.IsReady = true;
                                    order.IsReady = true;
                                }
                                ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, keyOrder, packageOrderDetail);

                                if (packageOrderDetail.All(x => x.IsReady) is true)
                                {
                                    var orderDb = await _unitOfWork.Repository<Order>().GetAll().FirstOrDefaultAsync(x => x.Id == order.OrderId);
                                    orderDb.OrderStatus = (int)OrderStatusEnum.FinishPrepare;

                                    await _unitOfWork.Repository<Order>().UpdateDetached(orderDb);
                                    await _unitOfWork.CommitAsync();
                                }
                            }
                            product.WaitingQuantity = numberOfConfirm;
                        }

                        packageResponse.PackageStations = new List<PackageStationResponse>();
                        HashSet<Guid> listStationId = new HashSet<Guid>(packageResponse.ProductTotalDetails
                                                                         .SelectMany(item => item.ProductDetails)
                                                                         .Select(product => product.StationId));
                        foreach (var stationId in listStationId)
                        {
                            var station = await _unitOfWork.Repository<Station>().GetAll().FirstOrDefaultAsync(x => x.Id == stationId);
                            var stationPackage = new PackageStationResponse()
                            {
                                StationId = stationId,
                                StationName = station.Name,
                                TotalQuantity = 0,
                                ReadyQuantity = 0,
                                IsShipperAssign = false,
                                PackageStationDetails = new List<PackageDetailResponse>(),
                                ListPackageMissing = new List<PackageDetailResponse>(),
                            };
                            foreach (var item in packageResponse.ProductTotalDetails)
                            {

                                var listProductGroupByStation = item.ProductDetails.Where(x => x.StationId == stationId).ToList();

                                var listProductReadyByStation = listProductGroupByStation.Where(x => x.IsReady == true)
                                                                                        .Select(x => new PackageDetailResponse()
                                                                                        {
                                                                                            ProductId = item.ProductId,
                                                                                            ProductName = item.ProductName,
                                                                                            Quantity = x.Quantity
                                                                                        }).ToList();

                                var listProductMissingByStation = listProductGroupByStation.Where(x => x.IsReady == false)
                                                                                       .Select(x => new PackageDetailResponse()
                                                                                       {
                                                                                           ProductId = item.ProductId,
                                                                                           ProductName = item.ProductName,
                                                                                           Quantity = x.Quantity
                                                                                       }).ToList();

                                stationPackage.TotalQuantity += listProductGroupByStation.Count();
                                stationPackage.ReadyQuantity += listProductReadyByStation.Count();

                                stationPackage.PackageStationDetails.AddRange(listProductReadyByStation);
                                stationPackage.ListPackageMissing.AddRange(listProductMissingByStation);
                            }
                            packageResponse.PackageStations.Add(stationPackage);
                        }
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
                            var packageError = packageResponse.ErrorProducts.Where(x => x.ProductId == Guid.Parse(item)).FirstOrDefault();
                            packageResponse.ErrorProducts.Remove(packageError);

                            var product = packageResponse.ProductTotalDetails.Find(x => x.ProductId == Guid.Parse(item));

                            packageResponse.TotalProductError -= (int)request.Quantity;
                            packageResponse.TotalProductReady += (int)request.Quantity;

                            product.ReadyQuantity += (int)request.Quantity;
                            product.ErrorQuantity -= (int)request.Quantity;

                            var listOrder = product.ProductDetails.Where(x => x.IsReady == false).OrderByDescending(x => x.CheckInDate);

                            var numberOfConfirm = request.Quantity + product.WaitingQuantity;
                            foreach (var order in listOrder)
                            {
                                var keyOrder = RedisDbEnum.OrderOperation.GetDisplayName() + ":" + order.OrderCode;
                                var orderValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, keyOrder, null);
                                List<PackageOrderDetailModel> packageOrderDetail = JsonConvert.DeserializeObject<List<PackageOrderDetailModel>>(orderValue);

                                var productInOrder = packageOrderDetail.FirstOrDefault(x => x.ProductId == Guid.Parse(item));
                                if (numberOfConfirm >= productInOrder.Quantity)
                                {
                                    numberOfConfirm -= productInOrder.Quantity;
                                    productInOrder.IsReady = true;
                                }

                                if (packageOrderDetail.All(x => x.IsReady) is true)
                                {
                                    var orderDb = await _unitOfWork.Repository<Order>().GetAll().FirstOrDefaultAsync(x => x.Id == order.OrderId);
                                    orderDb.OrderStatus = (int)OrderStatusEnum.FinishPrepare;

                                    await _unitOfWork.Repository<Order>().UpdateDetached(orderDb);
                                    await _unitOfWork.CommitAsync();
                                }
                            }
                            product.WaitingQuantity = (int)numberOfConfirm;
                        }
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
