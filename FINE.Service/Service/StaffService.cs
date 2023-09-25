using AutoMapper;
using AutoMapper.QueryableExtensions;
using Castle.Core.Resource;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Attributes;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Order;
using FINE.Service.DTO.Request.Shipper;
using FINE.Service.DTO.Request.Staff;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Helpers;
using FINE.Service.Utilities;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NetTopologySuite.Algorithm;
using StackExchange.Redis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IStaffService
    {
        //Task<BaseResponsePagingViewModel<StaffResponse>> GetStaffs(StaffResponse filter, PagingRequest paging);
        Task<BaseResponseViewModel<StaffResponse>> GetStaffById(string staffId);
        Task<BaseResponseViewModel<dynamic>> Login(LoginRequest request);
        Task<BaseResponseViewModel<StaffResponse>> CreateAdminManager(CreateStaffRequest request);
        //Task<BaseResponseViewModel<StaffResponse>> UpdateStaff(string staffId, UpdateStaffRequest request);
        Task<BaseResponseViewModel<OrderResponse>> UpdateOrderStatus(string orderId, UpdateOrderStatusRequest request);
        Task<BaseResponseViewModel<SimulateResponse>> SimulateOrder(SimulateRequest request);
        Task<BaseResponsePagingViewModel<ReportMissingProductResponse>> GetReportMissingProduct(string storeId, string timeslotId);
        Task<BaseResponseViewModel<ShipperResponse>> UpdateMissingProduct(List<UpdateMissingProductRequest> request);

    }

    public class StaffService : IStaffService
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;
        private readonly IFcmTokenService _customerFcmtokenService;
        private readonly string _fineSugar;
        private readonly IOrderService _orderService;


        public StaffService(IMapper mapper, IUnitOfWork unitOfWork, IConfiguration config, IFcmTokenService customerFcmtokenService, IOrderService orderService)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _config = config;
            _fineSugar = _config["FineSugar"];
            _customerFcmtokenService = customerFcmtokenService;
            _orderService = orderService;
        }

        public async Task<BaseResponseViewModel<StaffResponse>> CreateAdminManager(CreateStaffRequest request)
        {
            try
            {
                var checkStaff = _unitOfWork.Repository<Staff>().Find(x => x.Username.Contains(request.Username));

                if (checkStaff != null)
                    throw new ErrorResponse(404, (int)StaffErrorEnum.STAFF_EXSIST,
                                        StaffErrorEnum.STAFF_EXSIST.GetDisplayName());

                var staff = _mapper.Map<CreateStaffRequest, Staff>(request);

                staff.Id = Guid.NewGuid();
                staff.Password = Utils.GetHash(request.Pass, _fineSugar);
                staff.CreateAt = DateTime.Now;
                staff.IsActive = true;

                await _unitOfWork.Repository<Staff>().InsertAsync(staff);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<StaffResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<StaffResponse>(staff)
                };
            }
            catch (ErrorResponse ex)
            {
                throw;
            }
        }

        public async Task<BaseResponseViewModel<StaffResponse>> GetStaffById(string id)
        {
            var staffId = Guid.Parse(id);
            var staff = _unitOfWork.Repository<Staff>().GetAll()
                                   .FirstOrDefault(x => x.Id == staffId);
            if (staff == null)
            {
                throw new ErrorResponse(404, (int)StaffErrorEnum.NOT_FOUND,
                                    StaffErrorEnum.NOT_FOUND.GetDisplayName());
            }
            return new BaseResponseViewModel<StaffResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<StaffResponse>(staff)
            };
        }

        //public async Task<BaseResponsePagingViewModel<StaffResponse>> GetStaffs(StaffResponse filter, PagingRequest paging)
        //{
        //    var staff = _unitOfWork.Repository<Staff>().GetAll()
        //                           .ProjectTo<StaffResponse>(_mapper.ConfigurationProvider)
        //                           .DynamicFilter(filter)
        //                           .DynamicSort(filter)
        //                           .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging,
        //                            Constants.DefaultPaging);
        //    return new BaseResponsePagingViewModel<StaffResponse>()
        //    {
        //        Metadata = new PagingsMetadata()
        //        {
        //            Page = paging.Page,
        //            Size = paging.PageSize,
        //            Total = staff.Item1
        //        },
        //        Data = staff.Item2.ToList()
        //    };
        //}

        public async Task<BaseResponseViewModel<dynamic>> Login(LoginRequest request)
        {
            try
            {
                var staff = _unitOfWork.Repository<Staff>().GetAll()
                                        .Where(x => x.Username.Equals(request.UserName))
                                        .FirstOrDefault();

                if (staff == null || !Utils.CompareHash(request.Password, staff.Password, _fineSugar))
                    return new BaseResponseViewModel<dynamic>()
                    {
                        Status = new StatusViewModel()
                        {
                            Success = false,
                            Message = StaffErrorEnum.LOGIN_FAIL.GetDisplayName(),
                            ErrorCode = (int)StaffErrorEnum.LOGIN_FAIL
                        }
                    };

                if (staff.RoleType == (int)SystemRoleTypeEnum.StoreManager)
                {
                    if (staff.StoreId == null)
                    {
                        return new BaseResponseViewModel<dynamic>()
                        {
                            Status = new StatusViewModel()
                            {
                                Success = false,
                                Message = StoreErrorEnums.NOT_FOUND.GetDisplayName(),
                                ErrorCode = (int)StoreErrorEnums.NOT_FOUND
                            }
                        };
                    }
                }

                //if (request.FcmToken != null && request.FcmToken.Trim().Length > 0)
                //    _customerFcmtokenService.AddStaffFcmToken(request.FcmToken, staff.Id);

                var token = AccessTokenManager.GenerateJwtToken1(staff.Name, staff.RoleType, staff.Id, _config);
                return new BaseResponseViewModel<dynamic>()
                {
                    Status = new StatusViewModel()
                    {
                        Success = true,
                        Message = "Success",
                        ErrorCode = 0
                    },
                    Data = new
                    {
                        AccessToken = token,
                        Name = staff.Name,
                        Roles = staff.RoleType
                    }
                };
            }
            catch (ErrorResponse ex)
            {
                throw;
            }
        }

        //public async Task<BaseResponseViewModel<StaffResponse>> UpdateStaff(string id, UpdateStaffRequest request)
        //{
        //    var staffId = Guid.Parse(id);
        //    var staff = _unitOfWork.Repository<Staff>().Find(x => x.Id == staffId);
        //    if (staff == null)
        //    {
        //        throw new ErrorResponse(404, (int)StaffErrorEnum.NOT_FOUND,
        //                            StaffErrorEnum.NOT_FOUND.GetDisplayName());
        //    }
        //    var staffMappingResult = _mapper.Map<UpdateStaffRequest, Staff>(request, staff);
        //    staffMappingResult.UpdateAt = DateTime.Now;
        //    staffMappingResult.Password = Utils.GetHash(request.Pass, _fineSugar);

        //    await _unitOfWork.Repository<Staff>()
        //                    .UpdateDetached(staffMappingResult);
        //    await _unitOfWork.CommitAsync();
        //    return new BaseResponseViewModel<StaffResponse>()
        //    {
        //        Status = new StatusViewModel()
        //        {
        //            Message = "Success",
        //            Success = true,
        //            ErrorCode = 0
        //        },
        //        Data = _mapper.Map<StaffResponse>(staffMappingResult)
        //    };
        //}
        public async Task<BaseResponseViewModel<OrderResponse>> UpdateOrderStatus(string orderId, UpdateOrderStatusRequest request)
        {
            try
            {
                var order = await _unitOfWork.Repository<Data.Entity.Order>().GetAll()
                                    .FirstOrDefaultAsync(x => x.Id == Guid.Parse(orderId));
                if (order == null)
                {
                    throw new ErrorResponse(404, (int)OrderErrorEnums.NOT_FOUND,
                                       OrderErrorEnums.NOT_FOUND.GetDisplayName());
                }

                var updateOrderStatus = _mapper.Map<UpdateOrderStatusRequest, Data.Entity.Order>(request, order);

                updateOrderStatus.UpdateAt = DateTime.Now;

                await _unitOfWork.Repository<Data.Entity.Order>().UpdateDetached(updateOrderStatus);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<OrderResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0,
                    },
                    Data = _mapper.Map<OrderResponse>(updateOrderStatus)
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
        public async Task<BaseResponsePagingViewModel<ReportMissingProductResponse>> GetReportMissingProduct(string storeId, string timeslotId)
        {
            try 
            {
                List<ReportMissingProductResponse> reportResponse = await ServiceHelpers.GetSetDataRedisReportMissingProduct(RedisSetUpType.GET);
                var reportByStore = reportResponse.Where(x => x.StoreId == Guid.Parse(storeId)
                                                  && x.TimeSlotId == Guid.Parse(timeslotId));

                return new BaseResponsePagingViewModel<ReportMissingProductResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Total = reportByStore.Count()
                    },
                    Data = reportByStore.ToList()
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<ShipperResponse>> UpdateMissingProduct(List<UpdateMissingProductRequest> request)
        {
            try 
            {
                foreach (var item in request)
                {
                    ServiceHelpers.GetSetDataRedisReportMissingProduct(RedisSetUpType.DELETE, item.ReportId.ToString());
                }

                return new BaseResponseViewModel<ShipperResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                };
            }
            catch(ErrorResponse ex)
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
                var getAllCustomer = await _unitOfWork.Repository<Customer>().GetAll().ToListAsync();

                #region Single Order
                if (request.SingleOrder is not null)
                {
                    var listCustomer = getAllCustomer
                                            .OrderBy(x => rand.Next())
                                            .Take((int)request.SingleOrder.TotalCustomer)
                                            .ToList();
                    var totalSingleOrderSuccess = request.SingleOrder.TotalOrderSuccess;
                    var totalSingleOrderFailed = request.SingleOrder.TotalOrder - request.SingleOrder.TotalOrderSuccess;
                    for (int quantity = 1; quantity <= request.SingleOrder.TotalOrder; quantity++)
                    {
                        //foreach (var customer in listCustomer)
                        int customerIndex = rand.Next(0, listCustomer.Count() - 1);
                        var customer = listCustomer.ElementAt(customerIndex);

                        if (totalSingleOrderSuccess > 0 && totalSingleOrderFailed > 0)
                        {
                            if (rand.Next() % 2 == 0)
                            {
                                var payload = new CreatePreOrderRequest
                                {
                                    OrderType = OrderTypeEnum.OrderToday,
                                    TimeSlotId = Guid.Parse(request.TimeSlotId),
                                };

                                payload.OrderDetails = productInMenu
                                    .OrderBy(x => rand.Next())
                                    .Take(3)
                                    .Select(x => new CreatePreOrderDetailRequest
                                    {
                                        ProductId = x.Id,
                                        Quantity = 1
                                    })
                                    .ToList();
                                try
                                {
                                    var rs = _orderService.CreatePreOrder(customer.Id.ToString(), payload).Result.Data;
                                    //var rs = _orderService.CreatePreOrder("DBC730A2-5563-40A4-B86F-D0074D289109", payload).Result.Data;
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
                                        var result = _orderService.CreateOrder(customer.Id.ToString(), payloadCreateOrder).Result.Data;
                                        //var result = _orderService.CreateOrder("DBC730A2-5563-40A4-B86F-D0074D289109", payloadCreateOrder).Result.Data;

                                        var orderSuccess = new OrderSuccess
                                        {
                                            Id = result.Id,
                                            OrderCode = result.OrderCode,
                                            Customer = result.Customer,
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
                                    totalSingleOrderSuccess--;
                                }
                                catch (Exception ex)
                                {
                                    var randomCustomer = listCustomer.OrderBy(x => rand.Next()).FirstOrDefault();

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
                            else
                            {
                                var randomCustomer = listCustomer.OrderBy(x => rand.Next()).FirstOrDefault();
                                if (!Utils.CheckTimeSlot(timeslot))
                                {
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
                                            Message = "Out of Time Slot!",
                                            Success = false,
                                            ErrorCode = 400,
                                        }
                                    };
                                    response.SingleOrderResult.OrderFailed.Add(orderFail);
                                }
                                else
                                {
                                    int randomError = rand.Next(1, 2);
                                    if (randomError == (int)SimulateOrderFailedType.Payment)
                                    {
                                        var orderFail = new OrderFailed
                                        {
                                            OrderCode = DateTime.Now.ToString("ddMMyy_HHmm") + "-" + Utils.GenerateRandomCode(5) + "-" + "1245020C-759F-4C5A-9F23-539B9673D523",
                                            Customer = new CustomerOrderResponse
                                            {
                                                Id = Guid.Parse("1245020C-759F-4C5A-9F23-539B9673D523"),
                                                CustomerCode = "1245020c-759f-4c5a-9f23-539b9673d523_03092023",
                                                Email = "",
                                                Name = "Di Na Cuteeeee",
                                                Phone = "+84838073755",
                                            },
                                            Status = new StatusViewModel
                                            {
                                                Message = "Balance is not enough",
                                                Success = false,
                                                ErrorCode = 400,
                                            }
                                        };
                                        response.SingleOrderResult.OrderFailed.Add(orderFail);
                                    }
                                    else if (randomError == (int)SimulateOrderFailedType.OutOfProduct)
                                    {
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
                                                Message = "This product is not avaliable!",
                                                Success = false,
                                                ErrorCode = 400,
                                            }
                                        };
                                        response.SingleOrderResult.OrderFailed.Add(orderFail);
                                    }
                                }
                                totalSingleOrderFailed--;
                            }
                        }
                        else if (totalSingleOrderSuccess > 0)
                        {
                            var payload = new CreatePreOrderRequest
                            {
                                OrderType = OrderTypeEnum.OrderToday,
                                TimeSlotId = Guid.Parse(request.TimeSlotId),
                            };


                            payload.OrderDetails = productInMenu
                                .Take(3)
                                .Select(x => new CreatePreOrderDetailRequest
                                {
                                    ProductId = x.Id,
                                    Quantity = 1
                                })
                                .ToList();
                            try
                            {
                                var rs = _orderService.CreatePreOrder(customer.Id.ToString(), payload).Result.Data;
                                //var rs = _orderService.CreatePreOrder("DBC730A2-5563-40A4-B86F-D0074D289109", payload).Result.Data;
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
                                    var result = _orderService.CreateOrder(customer.Id.ToString(), payloadCreateOrder).Result.Data;
                                    //var result = _orderService.CreateOrder("DBC730A2-5563-40A4-B86F-D0074D289109", payloadCreateOrder).Result.Data;
                                    var orderSuccess = new OrderSuccess
                                    {
                                        Id = result.Id,
                                        OrderCode = result.OrderCode,
                                        Customer = result.Customer,
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
                                totalSingleOrderSuccess--;
                            }
                            catch (Exception ex)
                            {
                                var randomCustomer = listCustomer.OrderBy(x => rand.Next()).FirstOrDefault();
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
                        else if (totalSingleOrderFailed > 0)
                        {
                            var randomCustomer = listCustomer.OrderBy(x => rand.Next()).FirstOrDefault();
                            if (!Utils.CheckTimeSlot(timeslot))
                            {
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
                                        Message = "Out of Time Slot!",
                                        Success = false,
                                        ErrorCode = 400,
                                    }
                                };
                                response.SingleOrderResult.OrderFailed.Add(orderFail);
                            }
                            else
                            {
                                int randomError = rand.Next(1, 2);

                                if (randomError == (int)SimulateOrderFailedType.Payment)
                                {
                                    var orderFail = new OrderFailed
                                    {
                                        OrderCode = DateTime.Now.ToString("ddMMyy_HHmm") + "-" + Utils.GenerateRandomCode(5) + "-" + "1245020C-759F-4C5A-9F23-539B9673D523",
                                        Customer = new CustomerOrderResponse
                                        {
                                            Id = Guid.Parse("1245020C-759F-4C5A-9F23-539B9673D523"),
                                            CustomerCode = "1245020c-759f-4c5a-9f23-539b9673d523_03092023",
                                            Email = "",
                                            Name = "Di Na Cuteeeee",
                                            Phone = "+84838073755",
                                        },
                                        Status = new StatusViewModel
                                        {
                                            Message = "Balance is not enough",
                                            Success = false,
                                            ErrorCode = 400,
                                        }
                                    };
                                    response.SingleOrderResult.OrderFailed.Add(orderFail);
                                }
                                else if (randomError == (int)SimulateOrderFailedType.OutOfProduct)
                                {
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
                                            Message = "This product is not avaliable!",
                                            Success = false,
                                            ErrorCode = 400,
                                        }
                                    };
                                    response.SingleOrderResult.OrderFailed.Add(orderFail);
                                }
                            }
                            totalSingleOrderFailed--;

                        }

                    }
                }
                #endregion

                #region CoOrder
                if (request.CoOrder is not null)
                {
                    var listCustomer = getAllCustomer
                                           .OrderBy(x => rand.Next())
                                           .Take((int)request.CoOrder.CustomerEach)
                                           .ToList();
                    var openCoOrderCustomer = listCustomer.OrderBy(x => rand.Next()).Take(1).FirstOrDefault();
                    var restCustomers = listCustomer.Skip(1).ToList();

                    var totalCoOrderSuccess = request.CoOrder.TotalOrderSuccess;
                    var totalCoOrderFailed = request.CoOrder.TotalOrder - request.CoOrder.TotalOrderSuccess;

                    for (int quantity = 1; quantity <= request.CoOrder.TotalOrder; quantity++)
                    {
                        if (totalCoOrderSuccess > 0 && totalCoOrderFailed > 0)
                        {
                            if (rand.Next() % 2 == 0)
                            {
                                var payloadPreOrder = new CreatePreOrderRequest
                                {
                                    OrderType = OrderTypeEnum.OrderToday,
                                    TimeSlotId = Guid.Parse(request.TimeSlotId),
                                    PartyType = PartyOrderType.CoOrder
                                };

                                payloadPreOrder.OrderDetails = productInMenu
                                    .OrderBy(x => rand.Next())
                                    .Take(2)
                                    .Select(x => new CreatePreOrderDetailRequest
                                    {
                                        ProductId = x.Id,
                                        Quantity = 1
                                    })
                                    .ToList();

                                var openCoOrder = _orderService.OpenCoOrder(openCoOrderCustomer.Id.ToString(), payloadPreOrder);

                                try
                                {
                                    foreach (var cus in restCustomers)
                                    {
                                        var joinCoOrder = _orderService.JoinPartyOrder(cus.Id.ToString(), openCoOrder.Result.Data.PartyCode);
                                        var cusPayloadPreOrder = new CreatePreOrderRequest
                                        {
                                            OrderType = OrderTypeEnum.OrderToday,
                                            TimeSlotId = Guid.Parse(request.TimeSlotId),
                                            PartyType = PartyOrderType.CoOrder
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
                                        var addProduct = _orderService.AddProductIntoPartyCode(cus.Id.ToString(), openCoOrder.Result.Data.PartyCode, cusPayloadPreOrder);
                                        var confirmCoOrder = _orderService.FinalConfirmCoOrder(cus.Id.ToString(), openCoOrder.Result.Data.PartyCode);
                                    }


                                    var preCoOrder = _orderService.CreatePreCoOrder(openCoOrderCustomer.Id.ToString(), (OrderTypeEnum)payloadPreOrder.OrderType, openCoOrder.Result.Data.PartyCode).Result.Data;

                                    var stationId = station
                                                        .OrderBy(x => rand.Next())
                                                        .Select(x => x.Id)
                                                        .FirstOrDefault();

                                    var payloadCreateOrder = new CreateOrderRequest()
                                    {
                                        Id = preCoOrder.Id,
                                        OrderCode = preCoOrder.OrderCode,
                                        PartyCode = openCoOrder.Result.Data.PartyCode,
                                        TotalAmount = preCoOrder.TotalAmount,
                                        FinalAmount = preCoOrder.FinalAmount,
                                        TotalOtherAmount = preCoOrder.TotalOtherAmount,
                                        OrderType = (OrderTypeEnum)preCoOrder.OrderType,
                                        TimeSlotId = Guid.Parse(request.TimeSlotId),
                                        StationId = stationId.ToString(),
                                        PaymentType = PaymentTypeEnum.FineWallet,
                                        IsPartyMode = true,
                                        ItemQuantity = preCoOrder.ItemQuantity,
                                        Point = preCoOrder.Point,
                                        OrderDetails = preCoOrder.OrderDetails.Select(detail => new CreateOrderDetail()
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
                                        OtherAmounts = preCoOrder.OtherAmounts
                                    };
                                    try
                                    {
                                        var result = _orderService.CreateOrder(openCoOrderCustomer.Id.ToString(), payloadCreateOrder).Result.Data;

                                        var orderSuccess = new OrderSuccess
                                        {
                                            Id = result.Id,
                                            OrderCode = result.OrderCode,
                                            Customer = result.Customer,
                                        };
                                        response.CoOrderOrderResult.OrderSuccess.Add(orderSuccess);
                                    }
                                    catch (Exception ex)
                                    {
                                        ErrorResponse err = (ErrorResponse)ex.InnerException;
                                        var orderFail = new OrderFailed
                                        {
                                            OrderCode = preCoOrder.OrderCode,
                                            Customer = preCoOrder.Customer,
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
                                totalCoOrderSuccess--;
                            }
                            else
                            {
                                var randomCustomer = listCustomer.OrderBy(x => rand.Next()).FirstOrDefault();
                                if (!Utils.CheckTimeSlot(timeslot))
                                {
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
                                            Message = "Out of Time Slot!",
                                            Success = false,
                                            ErrorCode = 400,
                                        }
                                    };
                                    response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                                }
                                else
                                {
                                    int randomError = rand.Next(1, 2);
                                    if (randomError == (int)SimulateOrderFailedType.Payment)
                                    {
                                        var orderFail = new OrderFailed
                                        {
                                            OrderCode = DateTime.Now.ToString("ddMMyy_HHmm") + "-" + Utils.GenerateRandomCode(5) + "-" + "1245020C-759F-4C5A-9F23-539B9673D523",
                                            Customer = new CustomerOrderResponse
                                            {
                                                Id = Guid.Parse("1245020C-759F-4C5A-9F23-539B9673D523"),
                                                CustomerCode = "1245020c-759f-4c5a-9f23-539b9673d523_03092023",
                                                Email = "",
                                                Name = "Di Na Cuteeeee",
                                                Phone = "+84838073755",
                                            },
                                            Status = new StatusViewModel
                                            {
                                                Message = "Balance is not enough",
                                                Success = false,
                                                ErrorCode = 400,
                                            }
                                        };
                                        response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                                    }
                                    else if (randomError == (int)SimulateOrderFailedType.OutOfProduct)
                                    {
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
                                                Message = "This product is not avaliable!",
                                                Success = false,
                                                ErrorCode = 400,
                                            }
                                        };
                                        response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                                    }
                                }
                                totalCoOrderFailed--;
                            }
                        }
                        else if (totalCoOrderSuccess > 0)
                        {

                            var payloadPreOrder = new CreatePreOrderRequest
                            {
                                OrderType = OrderTypeEnum.OrderToday,
                                TimeSlotId = Guid.Parse(request.TimeSlotId),
                                PartyType = PartyOrderType.CoOrder
                            };

                            payloadPreOrder.OrderDetails = productInMenu
                                .OrderBy(x => rand.Next())
                                .Take(2)
                                .Select(x => new CreatePreOrderDetailRequest
                                {
                                    ProductId = x.Id,
                                    Quantity = 1
                                })
                                .ToList();

                            var openCoOrder = _orderService.OpenCoOrder(openCoOrderCustomer.Id.ToString(), payloadPreOrder);

                            try
                            {
                                foreach (var cus in restCustomers)
                                {
                                    var joinCoOrder = _orderService.JoinPartyOrder(cus.Id.ToString(), openCoOrder.Result.Data.PartyCode);
                                    var cusPayloadPreOrder = new CreatePreOrderRequest
                                    {
                                        OrderType = OrderTypeEnum.OrderToday,
                                        TimeSlotId = Guid.Parse(request.TimeSlotId),
                                        PartyType = PartyOrderType.CoOrder
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
                                    var addProduct = _orderService.AddProductIntoPartyCode(cus.Id.ToString(), openCoOrder.Result.Data.PartyCode, cusPayloadPreOrder);
                                    var confirmCoOrder = _orderService.FinalConfirmCoOrder(cus.Id.ToString(), openCoOrder.Result.Data.PartyCode);
                                }


                                var preCoOrder = _orderService.CreatePreCoOrder(openCoOrderCustomer.Id.ToString(), (OrderTypeEnum)payloadPreOrder.OrderType, openCoOrder.Result.Data.PartyCode).Result.Data;

                                var stationId = station
                                                    .OrderBy(x => rand.Next())
                                                    .Select(x => x.Id)
                                                    .FirstOrDefault();

                                var payloadCreateOrder = new CreateOrderRequest()
                                {
                                    Id = preCoOrder.Id,
                                    OrderCode = preCoOrder.OrderCode,
                                    PartyCode = openCoOrder.Result.Data.PartyCode,
                                    TotalAmount = preCoOrder.TotalAmount,
                                    FinalAmount = preCoOrder.FinalAmount,
                                    TotalOtherAmount = preCoOrder.TotalOtherAmount,
                                    OrderType = (OrderTypeEnum)preCoOrder.OrderType,
                                    TimeSlotId = Guid.Parse(request.TimeSlotId),
                                    StationId = stationId.ToString(),
                                    PaymentType = PaymentTypeEnum.FineWallet,
                                    IsPartyMode = true,
                                    ItemQuantity = preCoOrder.ItemQuantity,
                                    Point = preCoOrder.Point,
                                    OrderDetails = preCoOrder.OrderDetails.Select(detail => new CreateOrderDetail()
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
                                    OtherAmounts = preCoOrder.OtherAmounts
                                };
                                try
                                {
                                    var result = _orderService.CreateOrder(openCoOrderCustomer.Id.ToString(), payloadCreateOrder).Result.Data;

                                    var orderSuccess = new OrderSuccess
                                    {
                                        Id = result.Id,
                                        OrderCode = result.OrderCode,
                                        Customer = result.Customer,
                                    };
                                    response.CoOrderOrderResult.OrderSuccess.Add(orderSuccess);
                                }
                                catch (Exception ex)
                                {
                                    ErrorResponse err = (ErrorResponse)ex.InnerException;
                                    var orderFail = new OrderFailed
                                    {
                                        OrderCode = preCoOrder.OrderCode,
                                        Customer = preCoOrder.Customer,
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
                            totalCoOrderSuccess--;
                        }
                        else if (totalCoOrderFailed > 0)
                        {
                            var randomCustomer = listCustomer.OrderBy(x => rand.Next()).FirstOrDefault();
                            if (!Utils.CheckTimeSlot(timeslot))
                            {
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
                                        Message = "Out of Time Slot!",
                                        Success = false,
                                        ErrorCode = 400,
                                    }
                                };
                                response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                            }
                            else
                            {
                                int randomError = rand.Next(1, 2);
                                if (randomError == (int)SimulateOrderFailedType.Payment)
                                {
                                    var orderFail = new OrderFailed
                                    {
                                        OrderCode = DateTime.Now.ToString("ddMMyy_HHmm") + "-" + Utils.GenerateRandomCode(5) + "-" + "1245020C-759F-4C5A-9F23-539B9673D523",
                                        Customer = new CustomerOrderResponse
                                        {
                                            Id = Guid.Parse("1245020C-759F-4C5A-9F23-539B9673D523"),
                                            CustomerCode = "1245020c-759f-4c5a-9f23-539b9673d523_03092023",
                                            Email = "",
                                            Name = "Di Na Cuteeeee",
                                            Phone = "+84838073755",
                                        },
                                        Status = new StatusViewModel
                                        {
                                            Message = "Balance is not enough",
                                            Success = false,
                                            ErrorCode = 400,
                                        }
                                    };
                                    response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                                }
                                else if (randomError == (int)SimulateOrderFailedType.OutOfProduct)
                                {
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
                                            Message = "This product is not avaliable!",
                                            Success = false,
                                            ErrorCode = 400,
                                        }
                                    };
                                    response.CoOrderOrderResult.OrderFailed.Add(orderFail);
                                }
                            }
                            totalCoOrderFailed--;
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

    }
}
