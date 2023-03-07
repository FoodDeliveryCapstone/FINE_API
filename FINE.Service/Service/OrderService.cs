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
        //Task<BaseResponseViewModel<GenOrderResponse>> CreateOrder(CreateOrderRequest request);

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

                var timeSlot = _unitOfWork.Repository<TimeSlot>().Find(x => x.Id == request.TimeSlotId);
                var range = timeSlot.ArriveTime.Subtract(TimeSpan.FromMinutes(15));
                //if (DateTime.Now.TimeOfDay.CompareTo(range) > 0)
                //    throw new ErrorResponse(400, (int)TimeSlotErrorEnums.OUT_OF_TIMESLOT,
                //        TimeSlotErrorEnums.OUT_OF_TIMESLOT.GetDisplayName());

                #region Phân store trong order detail
                List<PreOrderDetailByStoreRequest> listODByStore = new List<PreOrderDetailByStoreRequest>();
                foreach (var orderDetailRequest in request.OrderDetails)
                {
                    #region check menu
                    var check = _unitOfWork.Repository<ProductInMenu>().GetAll()
                        .Include(x => x.Menu)
                        .Where(x => x.Id == orderDetailRequest.ProductInMenuId)
                        .FirstOrDefault();

                    if (check.Menu == null)
                        throw new ErrorResponse(404, (int)MenuErrorEnums.NOT_FOUND,
                           MenuErrorEnums.NOT_FOUND.GetDisplayName());
                    #endregion

                    if (!listODByStore.Any(x => x.StoreId == orderDetailRequest.StoreId))
                    {
                        PreOrderDetailByStoreRequest od = new PreOrderDetailByStoreRequest();
                        od.StoreId = orderDetailRequest.StoreId;

                        od.Details = new List<CreatePreOrderDetailRequest>();
                        od.Details.Add(orderDetailRequest);
                        listODByStore.Add(od);
                    }
                    else
                    {
                        listODByStore.Where(x => x.StoreId == orderDetailRequest.StoreId)
                            .FirstOrDefault().Details
                            .Add(orderDetailRequest);
                    }
                }
                #endregion

                //Create basic info for genOrder
                Order genOrder = new Order();
                genOrder.TotalAmount = 0;
                genOrder.ShippingFee = 0;

                #region Tách đơn  
                foreach (var storeOrderDetail in listODByStore)
                {
                    var order = _mapper.Map<Order>(request);

                    order.OrderCode = genOrder.OrderCode + "-" + storeOrderDetail.StoreId;
                    order.CheckInDate = DateTime.Now;

                    order.TotalAmount = 0;
                    foreach (var item in storeOrderDetail.Details)
                    {
                        order.TotalAmount += item.TotalAmount;
                    }

                    order.FinalAmount = order.TotalAmount;
                    order.OrderStatus = (int)OrderStatusEnum.PreOrder;
                    order.IsConfirm = false;

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

                genOrder = _mapper.Map<CreatePreOrderRequest, Order>(request, genOrder);

                var orderCount = _unitOfWork.Repository<Order>().GetAll()
                                            .Where(x => ((DateTime)x.CheckInDate).Date.Equals(DateTime.Now.Date)).Count() + 1;
                genOrder.OrderCode = customer.UniInfo.University.UniCode + "-" +
                                        orderCount.ToString().PadLeft(3, '0') + "." +
                                        Ultils.GenerateRandomCode();
                genOrder.CheckInDate = DateTime.Now;
                genOrder.FinalAmount = (double)(genOrder.TotalAmount + genOrder.ShippingFee);
                genOrder.OrderStatus = (int)OrderStatusEnum.PreOrder;
                genOrder.IsConfirm = false;
                genOrder.IsPartyMode = false;

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
