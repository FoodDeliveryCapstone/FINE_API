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
                List<PreOrderDetailByStoreRequest> listODByStore = new List<PreOrderDetailByStoreRequest>();

                foreach (var orderDetail in request.OrderDetails)
                {
                    #region check menu
                    var check = _unitOfWork.Repository<ProductInMenu>().GetAll()
                        .Include(x => x.Menu)
                        .Where(x => x.Id == orderDetail.ProductInMenuId)
                        .FirstOrDefault();

                    if (check.Menu == null)
                        throw new ErrorResponse(404, (int)MenuErrorEnums.NOT_FOUND,
                           MenuErrorEnums.NOT_FOUND.GetDisplayName());
                    #endregion

                    if (!listODByStore.Any(x => x.StoreId == orderDetail.StoreId))
                    {
                        PreOrderDetailByStoreRequest od = new PreOrderDetailByStoreRequest();
                        od.StoreId = orderDetail.StoreId;
                        od.StoreName = check.Product.Store.StoreName;
                        od.Details = new List<CreatePreOrderDetailRequest>();
                        od.Details.Add(orderDetail);
                        listODByStore.Add(od);
                    }
                    else
                    {
                        listODByStore.Where(x => x.StoreId == orderDetail.StoreId)
                            .FirstOrDefault().Details
                            .Add(orderDetail);
                    }
                }
                #endregion

                GenOrderResponse genOrder = new GenOrderResponse();

                var customer = await _unitOfWork.Repository<Customer>().GetById(request.CustomerId);

                var orderCount = _unitOfWork.Repository<Order>().GetAll()
                    .Where(x => ((DateTime)x.CheckInDate).Date.Equals(DateTime.Now.Date)).Count() + 1;
                genOrder.OrderCode = customer.UniInfo.University.UniCode + "-" +
                                        orderCount.ToString().PadLeft(3, '0') + "." + Ultils.GenerateRandomCode();

                #region Gen customer + delivery phone

                //check phone number
                if (!customer.Phone.Contains(request.DeliveryPhone))
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

              #region Tách order
                foreach (var store in listODByStore)
                {
                    var order = _mapper.Map<OrderResponse>(request);
                    order.OrderCode = genOrder.OrderCode + "-" + store.StoreId;
                    order.TotalAmount = 0;

                    genOrder.InverseGeneralOrder = new List<OrderResponse>();
                    foreach (var item in store.Details)
                    {
                        order.TotalAmount += item.TotalAmount;                       
                    }
                    order.FinalAmount = order.TotalAmount;
                    order.OrderStatus = (int)OrderStatusEnum.PreOrder;
                    order.StoreName = store.StoreName;

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
            GenOrderResponse genOrder = new GenOrderResponse();

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

            return null;
        }
    }
}
