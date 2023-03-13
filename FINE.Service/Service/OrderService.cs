using AutoMapper;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.DTO.Request.Order;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IOrderService
    {
        Task<BaseResponseViewModel<GenOrderResponse>> CreatePreOrder(CreatePreOrderRequest request);
        Task<BaseResponseViewModel<GenOrderResponse>> CreateOrder(CreateGenOrderRequest request);

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

        public async Task<BaseResponseViewModel<GenOrderResponse>> CreatePreOrder(CreatePreOrderRequest request)
        {
            try
            {
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
                        preOD.Details.Add(detail);
                        listPreOD.Add(preOD);
                    }
                    else
                    {
                        var preOD = listPreOD.Where(x => x.StoreId == productInMenu.StoreId)
                            .FirstOrDefault();
                        preOD.TotalAmount += detail.TotalAmount;
                        preOD.Details.Add(detail);
                    }
                }
                #endregion

                var genOrder = _mapper.Map<GenOrderResponse>(request);

                var customer = await _unitOfWork.Repository<Customer>().GetById(request.CustomerId);

                var orderCount = _unitOfWork.Repository<Order>().GetAll()
                    .Where(x => ((DateTime)x.CheckInDate).Date.Equals(DateTime.Now.Date)).Count() + 1;
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
                    order.OrderDetails = _mapper.Map<List<OrderDetailResponse>>(store.Details);
                    genOrder.InverseGeneralOrder.Add(order);

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

                #region Timeslot
                var timeSlot = _unitOfWork.Repository<TimeSlot>().Find(x => x.Id == request.TimeSlotId);

                var range = timeSlot.ArriveTime.Subtract(TimeSpan.FromMinutes(15));

                //if (DateTime.Now.TimeOfDay.CompareTo(range) > 0)
                //    throw new ErrorResponse(400, (int)TimeSlotErrorEnums.OUT_OF_TIMESLOT,
                //        TimeSlotErrorEnums.OUT_OF_TIMESLOT.GetDisplayName());

                genOrder.TimeSlot = _mapper.Map<OrderTimeSlotResponse>(timeSlot);
                #endregion

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

        public async Task<BaseResponseViewModel<GenOrderResponse>> CreateOrder(CreateGenOrderRequest request)
        {
            try
            {
                #region Customer
                var customer = await _unitOfWork.Repository<Customer>().GetById(request.CustomerId);

                //check phone number
                if (!customer.Phone.Contains(request.DeliveryPhone))
                {
                    var checkPhone = Ultils.CheckVNPhone(request.DeliveryPhone);
                    if (!checkPhone)
                        throw new ErrorResponse(400, (int)OrderErrorEnums.INVALID_PHONE_NUMBER,
                                                OrderErrorEnums.INVALID_PHONE_NUMBER.GetDisplayName());
                }
                if (!request.DeliveryPhone.StartsWith("+84"))
                {
                    request.DeliveryPhone = request.DeliveryPhone.TrimStart(new char[] { '0' });
                    request.DeliveryPhone = "+84" + request.DeliveryPhone;
                }
                #endregion

                //if (DateTime.Now.TimeOfDay.CompareTo(range) > 0)
                //    throw new ErrorResponse(400, (int)TimeSlotErrorEnums.OUT_OF_TIMESLOT,
                //        TimeSlotErrorEnums.OUT_OF_TIMESLOT.GetDisplayName());

                var genOrder = _mapper.Map<Order>(request);
                genOrder.CheckInDate = DateTime.Now;
                genOrder.OrderStatus = (int)OrderStatusEnum.PaymentPending;

                foreach(var order in genOrder.InverseGeneralOrder)
                {
                    order.DeliveryPhone = request.DeliveryPhone;
                    order.CheckInDate = DateTime.Now;
                    order.RoomId = request.RoomId;
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
    }
}

