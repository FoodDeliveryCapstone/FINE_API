﻿using AutoMapper;
using Azure.Core;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.DTO.Request.Package;
using FINE.Service.DTO.Response;
using FINE.Service.Helpers;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ServiceStack.Web;
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
        //Task<BaseResponseViewModel<List<PackageStationResponse>>> GetPackageForShipper(string staffId, string timeSlotId);
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

                var key = staff.Store.StoreName + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");

                var redisValue = await ServiceHelpers.GetSetDataRedis(RedisDbEnum.Staff, RedisSetUpType.GET, key, null);

                packageResponse = JsonConvert.DeserializeObject<PackageResponse>(redisValue);

                var packageStation = packageResponse.PackageStations.Where(x => x.IsShipperAssign == false).FirstOrDefault();
                packageStation.IsShipperAssign = true;

                var keyShipper = station.Code + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");
                var redisShipperValue = await ServiceHelpers.GetSetDataRedis(RedisDbEnum.Shipper, RedisSetUpType.GET, key, null);

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
                        PackageShipperDetails = new List<PackageShipperDetailResponse>()
                    };
                }
                foreach (var pack in packageStation.PackageStationDetails)
                {
                    packageShipper.PackageShipperDetails.Add(new PackageShipperDetailResponse()
                    {
                        ProductId = pack.ProductId,
                        ProductName = pack.ProductName,
                        Quantity = pack.Quantity,
                    });
                }
                ServiceHelpers.GetSetDataRedis(RedisDbEnum.Shipper, RedisSetUpType.SET, keyShipper, packageShipper);
                ServiceHelpers.GetSetDataRedis(RedisDbEnum.Staff, RedisSetUpType.SET, key, packageResponse);
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

                var key = staff.Store.StoreName + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");

                var redisValue = await ServiceHelpers.GetSetDataRedis(RedisDbEnum.Staff, RedisSetUpType.GET, key, null);
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

        //public async Task<BaseResponseViewModel<List<PackageStationResponse>>> GetPackageForShipper(string staffId, string timeSlotId)
        //{
        //    try
        //    {

        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

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

                var key = staff.Store.StoreName + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");

                var redisValue = await ServiceHelpers.GetSetDataRedis(RedisDbEnum.Staff, RedisSetUpType.GET, key, null);
                if (redisValue.HasValue == true)
                {
                    packageResponse = JsonConvert.DeserializeObject<PackageResponse>(redisValue);
                    result.AddRange(packageResponse.PackageStations);
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

                var key = staff.Store.StoreName + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");

                var redisValue = await ServiceHelpers.GetSetDataRedis(RedisDbEnum.Staff, RedisSetUpType.GET, key, null);
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

                            var listOrder = product.ProductDetails.Where(x => x.IsReady == false).OrderByDescending(x => x.CheckInDate);

                            foreach (var order in listOrder)
                            {
                                var orderValue = await ServiceHelpers.GetSetDataRedis(RedisDbEnum.OrderOperation, RedisSetUpType.GET, order.OrderId.ToString(), null);
                                List<PackageOrderDetailModel> packageOrderDetail = JsonConvert.DeserializeObject<List<PackageOrderDetailModel>>(orderValue);

                                var productInOrder = packageOrderDetail.FirstOrDefault(x => x.ProductId == Guid.Parse(item));
                                if (numberOfConfirm >= productInOrder.Quantity)
                                {
                                    numberOfConfirm -= productInOrder.Quantity;
                                    productInOrder.IsReady = true;
                                    order.IsReady = true;
                                }
                                ServiceHelpers.GetSetDataRedis(RedisDbEnum.OrderOperation, RedisSetUpType.SET, order.OrderId.ToString(), packageOrderDetail);

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
                        HashSet<Guid> listStationId = new HashSet<Guid>();
                        foreach (var item in packageResponse.ProductTotalDetails)
                        {
                            foreach (var product in item.ProductDetails)
                            {
                                listStationId.Add(product.StationId);
                            }
                        }
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
                                PackageStationDetails = new List<PackageStationDetailResponse>(),
                                ListPackageMissing = new List<PackageStationDetailResponse>(),
                            };
                            foreach (var item in packageResponse.ProductTotalDetails)
                            {

                                var listProductGroupByStation = item.ProductDetails.Where(x => x.StationId == stationId).ToList();

                                var listProductReadyByStation = listProductGroupByStation.Where(x => x.IsReady == true)
                                                                                        .Select(x => new PackageStationDetailResponse()
                                                                                        {
                                                                                            ProductId = item.ProductId,
                                                                                            ProductName = item.ProductName,
                                                                                            Quantity = x.Quantity
                                                                                        }).ToList();

                                var listProductMissingByStation = listProductGroupByStation.Where(x => x.IsReady == false)
                                                                                       .Select(x => new PackageStationDetailResponse()
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
                            var product = packageResponse.ProductTotalDetails.Find(x => x.ProductId == Guid.Parse(item));

                            packageResponse.TotalProductError -= (int)request.Quantity;
                            packageResponse.TotalProductReady += (int)request.Quantity;

                            product.ReadyQuantity += (int)request.Quantity;
                            product.ErrorQuantity -= (int)request.Quantity;

                            var listOrder = product.ProductDetails.Where(x => x.IsReady == false).OrderByDescending(x => x.CheckInDate);

                            var numberOfConfirm = request.Quantity + product.WaitingQuantity;
                            foreach (var order in listOrder)
                            {
                                var orderValue = await ServiceHelpers.GetSetDataRedis(RedisDbEnum.OrderOperation, RedisSetUpType.GET, order.OrderId.ToString(), null);
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
                ServiceHelpers.GetSetDataRedis(RedisDbEnum.Staff, RedisSetUpType.SET, key, packageResponse);

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
