using AutoMapper;
using AutoMapper.QueryableExtensions;
using Castle.Core.Resource;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Attributes;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Box;
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
        Task<BaseResponsePagingViewModel<StaffResponse>> GetStaffs(PagingRequest paging);
        Task<BaseResponseViewModel<StaffResponse>> GetStaffById(string staffId);
        Task<BaseResponseViewModel<dynamic>> Login(LoginRequest request);
        Task<BaseResponseViewModel<StaffResponse>> CreateAdminManager(CreateStaffRequest request);
        Task<BaseResponseViewModel<StaffResponse>> UpdateStaff(string staffId, UpdateStaffRequest request);
        Task<BaseResponseViewModel<OrderResponse>> UpdateOrderStatus(string orderId, UpdateOrderStatusRequest request);
        Task<BaseResponsePagingViewModel<ReportMissingProductResponse>> GetReportMissingProduct(string storeId, string timeslotId);
        Task<BaseResponseViewModel<ShipperResponse>> UpdateMissingProduct(List<UpdateMissingProductRequest> request);
        Task<BaseResponseViewModel<OrderResponse>> CreateOrderForSimulate(string customerId, CreateOrderRequest request);

    }

    public class StaffService : IStaffService
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;
        private readonly IFcmTokenService _customerFcmtokenService;
        private readonly string _fineSugar;
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly IBoxService _boxService;

        public StaffService(IMapper mapper, IUnitOfWork unitOfWork, IConfiguration config, IFcmTokenService customerFcmtokenService, IOrderService orderService, IPaymentService paymentService, IBoxService boxService)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _config = config;
            _fineSugar = _config["FineSugar"];
            _customerFcmtokenService = customerFcmtokenService;
            _orderService = orderService;
            _paymentService = paymentService;
            _boxService = boxService;
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

        public async Task<BaseResponsePagingViewModel<StaffResponse>> GetStaffs(PagingRequest paging)
        {
            var staff = _unitOfWork.Repository<Staff>().GetAll()
                                   .ProjectTo<StaffResponse>(_mapper.ConfigurationProvider)
                                   .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging,
                                    Constants.DefaultPaging);
            return new BaseResponsePagingViewModel<StaffResponse>()
            {
                Metadata = new PagingsMetadata()
                {
                    Page = paging.Page,
                    Size = paging.PageSize,
                    Total = staff.Item1
                },
                Data = staff.Item2.ToList()
            };
        }

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

        public async Task<BaseResponseViewModel<StaffResponse>> UpdateStaff(string staffId, UpdateStaffRequest request)
        {
            var staff = _unitOfWork.Repository<Staff>().Find(x => x.Id == Guid.Parse(staffId));
            if (staff == null)
            {
                throw new ErrorResponse(404, (int)StaffErrorEnum.NOT_FOUND,
                                    StaffErrorEnum.NOT_FOUND.GetDisplayName());
            }
            var staffMappingResult = _mapper.Map<UpdateStaffRequest, Staff>(request, staff);
            staffMappingResult.UpdateAt = DateTime.Now;
            staffMappingResult.Password = Utils.GetHash(request.Pass, _fineSugar);

            await _unitOfWork.Repository<Staff>().UpdateDetached(staffMappingResult);
            await _unitOfWork.CommitAsync();
            return new BaseResponseViewModel<StaffResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<StaffResponse>(staffMappingResult)
            };
        }
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
               
                List<ReportMissingProductResponse> reportsResponse = await ServiceHelpers.GetSetDataRedisReportMissingProduct(RedisSetUpType.GET);
                var reportsByStore = reportsResponse.Where(x => x.TimeSlotId == Guid.Parse(timeslotId)
                                                           && x.StoreId == Guid.Parse(storeId));
  
                return new BaseResponsePagingViewModel<ReportMissingProductResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Total = reportsByStore.Count()
                    },
                    Data = reportsByStore.ToList(),
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
        public async Task<BaseResponseViewModel<OrderResponse>> CreateOrderForSimulate(string customerId, CreateOrderRequest request)
        {
            try
            {
                var timeSlot = await _unitOfWork.Repository<TimeSlot>().FindAsync(x => x.Id == request.TimeSlotId);

                if (request.OrderType is OrderTypeEnum.OrderToday && !Utils.CheckTimeSlot(timeSlot))
                    throw new ErrorResponse(400, (int)TimeSlotErrorEnums.OUT_OF_TIMESLOT,
                        TimeSlotErrorEnums.OUT_OF_TIMESLOT.GetDisplayName());

                var customer = await _unitOfWork.Repository<Customer>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(customerId));

                if (customer.Phone is null)
                    throw new ErrorResponse(400, (int)CustomerErrorEnums.MISSING_PHONENUMBER,
                                            CustomerErrorEnums.MISSING_PHONENUMBER.GetDisplayName());

                var station = await _unitOfWork.Repository<Station>().GetAll()
                                        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(request.StationId));

                #region Check station in timeslot have available box 
                var getOrderInTimeslot = await _unitOfWork.Repository<Data.Entity.Order>().GetAll()
                                                .Where(x => x.TimeSlotId == request.TimeSlotId
                                                && x.StationId == Guid.Parse(request.StationId)
                                                && x.OrderStatus == (int)OrderStatusEnum.Processing
                                                && x.CheckInDate.Date == Utils.GetCurrentDatetime().Date).ToListAsync();

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

                if (request.PartyCode is not null)
                {
                    var checkCode = await _unitOfWork.Repository<Party>().GetAll()
                                    .FirstOrDefaultAsync(x => x.PartyCode == request.PartyCode);

                    if (checkCode == null)
                        throw new ErrorResponse(400, (int)PartyErrorEnums.INVALID_CODE, PartyErrorEnums.INVALID_CODE.GetDisplayName());

                    if (checkCode.Status is (int)PartyOrderStatus.CloseParty)
                        throw new ErrorResponse(404, (int)PartyErrorEnums.PARTY_CLOSED, PartyErrorEnums.PARTY_CLOSED.GetDisplayName());

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
                #region split order detail by store
                var orderDetailsByStore = resultOrder.OrderDetails
                  .GroupBy(x => x.StoreId)
                  .Select(group => new OrderByStoreResponse
                  {
                      OrderId = resultOrder.Id,
                      StoreId = group.Key,
                      CustomerName = resultOrder.Customer.Name,
                      TimeSlot = resultOrder.TimeSlot,
                      StationId = resultOrder.StationOrder.Id,
                      StationName = resultOrder.StationOrder.Name,
                      CheckInDate = order.CheckInDate,
                      OrderType = resultOrder.OrderType,
                      OrderDetailStoreStatus = OrderStatusEnum.Processing,
                      OrderDetails = _mapper.Map<List<OrderDetailResponse>, List<OrderDetailForStaffResponse>>
                                (resultOrder.OrderDetails.Where(x => x.StoreId == group.Key).ToList())
                                
                //resultOrder.OrderDetails.Where(x => x.StoreId == group.Key).ToList(),
            }).ToList();
                ServiceHelpers.GetSetDataRedisOrder(RedisSetUpType.SET, resultOrder.Id.ToString(), orderDetailsByStore);
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

    }
}
