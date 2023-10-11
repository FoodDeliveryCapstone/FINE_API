using AutoMapper;
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
        Task<BaseResponseViewModel<PackageResponse>> UpdatePackage(string staffId, UpdateProductPackageRequest request);
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

        public async Task<BaseResponseViewModel<PackageResponse>> GetPackage(string staffId, string timeSlotId)
        {
            try
            {
                PackageResponse packageResponse = new PackageResponse();
                var staff = await _unitOfWork.Repository<Staff>().GetAll()
                                         .FirstOrDefaultAsync(x => x.Id == Guid.Parse(staffId));

                var timeSlot = await _unitOfWork.Repository<TimeSlot>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(timeSlotId));

                var key = staff.Store.StoreName + "-" + timeSlot.ArriveTime;

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

                var key = staff.Store.StoreName + "-" + timeSlot.ArriveTime;

                var redisValue = await ServiceHelpers.GetSetDataRedis(RedisDbEnum.Staff, RedisSetUpType.GET, key, null);
                if (redisValue.HasValue == true)
                {
                    packageResponse = JsonConvert.DeserializeObject<PackageResponse>(redisValue);

                    HashSet<Guid> listStationId = new HashSet<Guid>();
                    foreach (var item in packageResponse.productTotalDetails)
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
                            PackageStationDetails = new List<PackageStationDetailResponse>()
                        };

                        foreach (var item in packageResponse.productTotalDetails)
                        {
                            var listProductGroupByStation = item.ProductDetails.Where(x => x.StationId == stationId)
                                                            .Select(x => new PackageStationDetailResponse()
                                                            {
                                                                ProductId = item.ProductId,
                                                                ProductName = item.ProductName,
                                                                Quantity = x.Quantity
                                                            })
                                                            .ToList();
                            
                        }
                        result.Add(stationPackage);
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

                var key = staff.Store.StoreName + "-" + timeSlot.ArriveTime;

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
                            var product = packageResponse.productTotalDetails.Find(x => x.ProductId == Guid.Parse(item));

                            packageResponse.TotalProductPending -= product.PendingQuantity;
                            packageResponse.TotalProductReady += product.PendingQuantity;

                            product.ReadyQuantity += product.PendingQuantity;
                            product.PendingQuantity = 0;

                            var listOrder = product.ProductDetails.OrderByDescending(x => x.CheckInDate);

                            var numberOfConfirm = product.PendingQuantity + product.WaitingQuantity;
                            foreach(var order in listOrder)
                            {
                                var orderValue = await ServiceHelpers.GetSetDataRedis(RedisDbEnum.OrderOperation, RedisSetUpType.GET, order.OrderId.ToString(), null);
                                List<PackageOrderDetailModel> packageOrderDetail = JsonConvert.DeserializeObject<List<PackageOrderDetailModel>>(orderValue);

                                var productInOrder = packageOrderDetail.FirstOrDefault(x => x.ProductId == Guid.Parse(item));
                                if(numberOfConfirm >= productInOrder.Quantity)
                                {
                                    numberOfConfirm -= productInOrder.Quantity;
                                    productInOrder.IsReady = true;
                                }

                                if(packageOrderDetail.All(x => x.IsReady) is true)
                                {
                                    var orderDb = await _unitOfWork.Repository<Order>().GetAll().FirstOrDefaultAsync(x => x.Id == order.OrderId);
                                    orderDb.OrderStatus = (int)OrderStatusEnum.StaffConfirm;

                                    await _unitOfWork.Repository<Order>().UpdateDetached(orderDb);
                                    await _unitOfWork.CommitAsync();
                                }
                            }
                            product.WaitingQuantity = numberOfConfirm;
                        }
                        break;

                    case PackageUpdateTypeEnum.Error:
                        foreach (var item in request.ProductsUpdate)
                        {
                            var product = packageResponse.productTotalDetails.Find(x => x.ProductId == Guid.Parse(item));

                            packageResponse.TotalProductError += (int)request.quantity;
                            packageResponse.TotalProductPending -= (int)request.quantity;

                            product.PendingQuantity -= (int)request.quantity;
                            product.ErrorQuantity += (int)request.quantity;
                        }
                        break;

                    case PackageUpdateTypeEnum.ReConfirm:
                        foreach (var item in request.ProductsUpdate)
                        {
                            var product = packageResponse.productTotalDetails.Find(x => x.ProductId == Guid.Parse(item));

                            packageResponse.TotalProductError -= (int)request.quantity;
                            packageResponse.TotalProductReady += (int)request.quantity;

                            product.ReadyQuantity += (int)request.quantity;
                            product.ErrorQuantity -= (int)request.quantity;

                            var listOrder = product.ProductDetails.OrderByDescending(x => x.CheckInDate);

                            var numberOfConfirm = request.quantity + product.WaitingQuantity;
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
                                    orderDb.OrderStatus = (int)OrderStatusEnum.StaffConfirm;

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
