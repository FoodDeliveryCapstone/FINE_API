using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Commons;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Order;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using NTQ.Sdk.Core.Utilities;
using static FINE.Service.Helpers.ErrorEnum;
using static FINE.Service.Helpers.Enum;
using Castle.Core.Resource;

namespace FINE.Service.Service
{
    public interface IOrderService
    {
        Task<BaseResponseViewModel<GenOrderResponse>> GetOrderById(int orderId);
        Task<BaseResponsePagingViewModel<GenOrderResponse>> GetOrders(PagingRequest paging);
        Task<BaseResponsePagingViewModel<GenOrderResponse>> GetOrderByCustomerId(int customerId, PagingRequest paging);
        Task<BaseResponseViewModel<GenOrderResponse>> CreatePreOrder(int customerId, CreatePreOrderRequest request);
        Task<BaseResponseViewModel<GenOrderResponse>> CreateOrder(int customerId, CreateGenOrderRequest request);
        Task<BaseResponseViewModel<GenOrderResponse>> UpdateCancelOrder(int orderId);
    }
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public OrderService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponsePagingViewModel<GenOrderResponse>> GetOrderByCustomerId(int customerId, PagingRequest paging)
        {
            try
            {
                var order = _unitOfWork.Repository<Order>().GetAll()
                                        .Where(x => x.CustomerId == customerId)
                                        .OrderByDescending(x => x.CheckInDate)
                                        .ProjectTo<GenOrderResponse>(_mapper.ConfigurationProvider)
                                        .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging,
                                        Constants.DefaultPaging);

                return new BaseResponsePagingViewModel<GenOrderResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Page = paging.Page,
                        Size = paging.PageSize,
                        Total = order.Item1
                    },
                    Data = order.Item2.ToList()
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<GenOrderResponse>> CreatePreOrder(int customerId, CreatePreOrderRequest request)
        {
            try
            {
                #region Timeslot
                var timeSlot = _unitOfWork.Repository<TimeSlot>().Find(x => x.Id == request.TimeSlotId);

                var range = timeSlot.ArriveTime.Subtract(TimeSpan.FromMinutes(15));

                //if (DateTime.Now.TimeOfDay.CompareTo(range) > 0)
                //    throw new ErrorResponse(400, (int)TimeSlotErrorEnums.OUT_OF_TIMESLOT,
                //        TimeSlotErrorEnums.OUT_OF_TIMESLOT.GetDisplayName());
                #endregion

                #region Phân store trong order detail
                List<ListDetailByStore> listPreOD = new List<ListDetailByStore>();
                foreach (var orderDetail in request.OrderDetails)
                {
                    var productInMenu = _unitOfWork.Repository<ProductInMenu>().GetAll()
                        .Include(x => x.Menu)
                        .Include(x => x.Product)
                        .Where(x => x.Id == orderDetail.ProductInMenuId)
                        .FirstOrDefault();

                    if (productInMenu.Menu == null)
                        throw new ErrorResponse(404, (int)MenuErrorEnums.NOT_FOUND,
                           MenuErrorEnums.NOT_FOUND.GetDisplayName());

                    var detail = new PreOrderDetailRequest();
                    detail.ProductInMenuId = orderDetail.ProductInMenuId;
                    detail.ProductCode = productInMenu.Product.ProductCode;
                    detail.ProductName = productInMenu.Product.ProductName;
                    detail.UnitPrice = productInMenu.Price;
                    detail.Quantity = orderDetail.Quantity;
                    detail.TotalAmount = (double)(detail.UnitPrice * detail.Quantity);
                    detail.FinalAmount = detail.TotalAmount;
                    detail.ComboId = orderDetail.ComboId;

                    //phan store
                    if (!listPreOD.Any(x => x.StoreId == productInMenu.StoreId))
                    {
                        ListDetailByStore preOD = new ListDetailByStore();
                        preOD.Details = new List<PreOrderDetailRequest>();
                        preOD.StoreId = (int)productInMenu.StoreId;
                        preOD.StoreName = productInMenu.Product.Store.StoreName;
                        preOD.TotalAmount = detail.TotalAmount;
                        preOD.TotalProduct = detail.Quantity;
                        preOD.Details.Add(detail);
                        listPreOD.Add(preOD);
                    }
                    else
                    {
                        var preOD = listPreOD.Where(x => x.StoreId == productInMenu.StoreId)
                            .FirstOrDefault();
                        preOD.TotalAmount += detail.TotalAmount;
                        preOD.TotalProduct += detail.Quantity;
                        preOD.Details.Add(detail);
                    }
                }
                #endregion

                var genOrder = _mapper.Map<GenOrderResponse>(request);

                var customer = await _unitOfWork.Repository<Customer>().GetById(customerId);

                var orderCount = _unitOfWork.Repository<Order>().GetAll().Count() + 1;
                genOrder.OrderCode = customer.UniInfo.University.UniCode + "-" +
                                        orderCount.ToString().PadLeft(3, '0') + "." + Ultils.GenerateRandomCode();

                #region Gen customer + delivery phone

                //check phone number
                if (customer.Phone.Contains(request.DeliveryPhone) == null)
                {
                    if (!Ultils.CheckVNPhone(request.DeliveryPhone))
                        throw new ErrorResponse(400, (int)OrderErrorEnums.INVALID_PHONE_NUMBER,
                                                OrderErrorEnums.INVALID_PHONE_NUMBER.GetDisplayName());
                }
                if (!request.DeliveryPhone.StartsWith("+84"))
                {
                    request.DeliveryPhone = request.DeliveryPhone.TrimStart(new char[] { '0' });
                    request.DeliveryPhone = "+84" + request.DeliveryPhone;
                }

                genOrder.Customer = _mapper.Map<OrderCustomerResponse>(customer);
                genOrder.DeliveryPhone = request.DeliveryPhone;
                #endregion

                genOrder.CheckInDate = DateTime.Now;

                #region FinalAmount / TotalAmount
                genOrder.InverseGeneralOrder = new List<OrderResponse>();

                foreach (var store in listPreOD)
                {
                    var order = _mapper.Map<OrderResponse>(store);
                    order.TotalAmount = store.TotalAmount;
                    order.OrderCode = genOrder.OrderCode + "-" + store.StoreId;
                    order.FinalAmount = order.TotalAmount;
                    order.OrderStatus = (int)OrderStatusEnum.PreOrder;
                    order.ItemQuantity = store.TotalProduct;
                    order.OrderDetails = _mapper.Map<List<OrderDetailResponse>>(store.Details);
                    genOrder.InverseGeneralOrder.Add(order);

                    genOrder.ItemQuantity += store.TotalProduct;
                    genOrder.TotalAmount += order.TotalAmount;
                    if (genOrder.ShippingFee == 0)
                    {
                        genOrder.ShippingFee = 5000;
                    }
                    else
                    {
                        genOrder.ShippingFee += 2000;
                    }
                }
                #endregion

                var room = _unitOfWork.Repository<Room>().GetAll().FirstOrDefault(x => x.Id == request.RoomId);
                genOrder.Room = _mapper.Map<OrderRoomResponse>(room);

                genOrder.FinalAmount = (double)(genOrder.TotalAmount + genOrder.ShippingFee);
                genOrder.OrderStatus = (int)OrderStatusEnum.PreOrder;
                genOrder.IsConfirm = false;
                genOrder.IsPartyMode = false;

                genOrder.CheckInDate = DateTime.Now;
                genOrder.TimeSlot = _mapper.Map<OrderTimeSlotResponse>(timeSlot);

                return new BaseResponseViewModel<GenOrderResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<GenOrderResponse>(genOrder)
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<GenOrderResponse>> CreateOrder(int customerId, CreateGenOrderRequest request)
        {
            try
            {
                #region Timeslot
                var timeSlot = _unitOfWork.Repository<TimeSlot>().Find(x => x.Id == request.TimeSlotId);

                var range = timeSlot.ArriveTime.Subtract(TimeSpan.FromMinutes(15));

                //if (DateTime.Now.TimeOfDay.CompareTo(range) > 0)
                //    throw new ErrorResponse(400, (int)TimeSlotErrorEnums.OUT_OF_TIMESLOT,
                //        TimeSlotErrorEnums.OUT_OF_TIMESLOT.GetDisplayName());
                #endregion

                #region Customer
                var customer = await _unitOfWork.Repository<Customer>().GetById(customerId);

                //check phone number
                if (customer.Phone.Contains(request.DeliveryPhone) == null)
                {
                    if (!Ultils.CheckVNPhone(request.DeliveryPhone))
                        throw new ErrorResponse(400, (int)OrderErrorEnums.INVALID_PHONE_NUMBER,
                                                OrderErrorEnums.INVALID_PHONE_NUMBER.GetDisplayName());
                }
                if (!request.DeliveryPhone.StartsWith("+84"))
                {
                    request.DeliveryPhone = request.DeliveryPhone.TrimStart(new char[] { '0' });
                    request.DeliveryPhone = "+84" + request.DeliveryPhone;
                }
                #endregion


                var genOrder = _mapper.Map<Order>(request);
                genOrder.CheckInDate = DateTime.Now;
                genOrder.CustomerId = customerId;
                genOrder.OrderStatus = (int)OrderStatusEnum.Processing;
                genOrder.IsConfirm = false;
                genOrder.IsPartyMode = false;

                foreach (var order in request.InverseGeneralOrder)
                {
                    var inverseOrder = new Order();
                    inverseOrder = _mapper.Map<Order>(genOrder);
                    inverseOrder = _mapper.Map<CreateOrderRequest, Order>(order, inverseOrder);
                    genOrder.InverseGeneralOrder.Add(inverseOrder);
                }

                await _unitOfWork.Repository<Order>().InsertAsync(genOrder);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<GenOrderResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<GenOrderResponse>(genOrder)
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<GenOrderResponse>> GetOrderById(int orderId)
        {
            try
            {
                var order = await _unitOfWork.Repository<Order>().GetAll()
                    .FirstOrDefaultAsync(x => x.Id == orderId);
                return new BaseResponseViewModel<GenOrderResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<GenOrderResponse>(order)
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<GenOrderResponse>> UpdateCancelOrder(int orderId)
        {
            try
            {
                var order = await _unitOfWork.Repository<Order>().GetAll()
                    .FirstOrDefaultAsync(x => x.Id == orderId);

                if (order == null)
                    throw new ErrorResponse(404, (int)OrderErrorEnums.NOT_FOUND_ID,
                          OrderErrorEnums.NOT_FOUND_ID.GetDisplayName());

                if (order.GeneralOrderId != null)
                {
                    var genOrder = await _unitOfWork.Repository<Order>().GetAll()
                    .FirstOrDefaultAsync(x => x.Id == order.GeneralOrderId);

                    if (genOrder.InverseGeneralOrder.Count() > 1)
                    {
                        genOrder.TotalAmount -= order.TotalAmount;
                        genOrder.Discount -= order.Discount;
                        genOrder.FinalAmount -= order.FinalAmount;
                        genOrder.ShippingFee -= 2000;
                    }
                    else
                    {
                        genOrder.OrderStatus = (int)OrderStatusEnum.UserCancel;
                    }
                    await _unitOfWork.Repository<Order>().UpdateDetached(genOrder);
                    await _unitOfWork.CommitAsync();
                } else
                {
                    foreach (var inverseOrder in order.InverseGeneralOrder)
                    {
                        inverseOrder.OrderStatus = (int)OrderStatusEnum.UserCancel;
                    }
                }
                order.OrderStatus = (int)OrderStatusEnum.UserCancel;

                await _unitOfWork.Repository<Order>().UpdateDetached(order);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<GenOrderResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    }
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponsePagingViewModel<GenOrderResponse>> GetOrders(PagingRequest paging)
        {
            try
            {
                var order = _unitOfWork.Repository<Order>().GetAll()
                                        .OrderByDescending(x => x.CheckInDate)
                                        .ProjectTo<GenOrderResponse>(_mapper.ConfigurationProvider)
                                        .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging,
                                        Constants.DefaultPaging);

                return new BaseResponsePagingViewModel<GenOrderResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Page = paging.Page,
                        Size = paging.PageSize,
                        Total = order.Item1
                    },
                    Data = order.Item2.ToList()
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}

