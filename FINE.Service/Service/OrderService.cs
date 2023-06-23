using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
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
using static FINE.Service.Helpers.ErrorEnum;
using static FINE.Service.Helpers.Enum;
using Castle.Core.Resource;
using System.Net.Mail;
using IronBarCode;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Hangfire.Server;
using Microsoft.Extensions.Configuration;
using Hangfire;
using FINE.Service.Attributes;

namespace FINE.Service.Service
{
    public interface IOrderService
    {
        Task<BaseResponseViewModel<GenOrderResponse>> GetOrderById(int orderId);
        Task<BaseResponsePagingViewModel<GenOrderResponse>> GetOrders(PagingRequest paging);
        Task<BaseResponsePagingViewModel<GenOrderResponse>> GetOrderByCustomerId(int customerId, PagingRequest paging);
        Task<BaseResponseViewModel<GenOrderResponse>> CreatePreOrder(int customerId, CreatePreOrderRequest request);
        Task<BaseResponseViewModel<dynamic>> CreateOrder(int customerId, CreateGenOrderRequest request);
        Task<BaseResponseViewModel<dynamic>> CreateCoOrder(int customerId, CreateGenOrderRequest request);
        Task<BaseResponseViewModel<dynamic>> UpdateOrder(int orderId, UpdateOrderTypeEnum orderStatus, UpdateOrderRequest request);
    }
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly INotifyService _notifyService;
        private readonly IConfiguration _configuration;
        private readonly IPaymentService _paymentService;

        public OrderService(IUnitOfWork unitOfWork, IMapper mapper, 
                            IConfiguration configuration, INotifyService notifyService,
                            IPaymentService paymentService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;
            _notifyService = notifyService;
            _paymentService = paymentService;
        }

        public async Task<string> CreateMailMessage(Order genOrder)
        {
            try
            {
                var body = "";
                var contentOrder = "";
                var contentOrderDetail = "";

                var path = Environment.CurrentDirectory + "\\Template";

                using (StreamReader reader = new StreamReader(Path.Combine(path, "EmailTemplate.html")))
                {
                    body = await reader.ReadToEndAsync();
                }
                using (StreamReader reader = new StreamReader(Path.Combine(path, "ContentOrder.html")))
                {
                    contentOrder = await reader.ReadToEndAsync();
                }
                using (StreamReader reader = new StreamReader(Path.Combine(path, "ContentOrderDetail.html")))
                {
                    contentOrderDetail = await reader.ReadToEndAsync();
                }
                foreach (var order in genOrder.InverseGeneralOrder)
                {
                    var storeName = _unitOfWork.Repository<Store>().GetAll().FirstOrDefault(x => x.Id == order.StoreId).StoreName;
                    foreach (var orderDetail in order.OrderDetails)
                    {
                        var orderDetailHTML = contentOrderDetail.Replace("{quan}", orderDetail.Quantity.ToString())
                            .Replace("{productName}", orderDetail.ProductName)
                            .Replace("{price}", orderDetail.TotalAmount.ToString());
                        contentOrderDetail += orderDetailHTML;
                    }
                    contentOrder = contentOrder.Replace("{StoreName}", storeName)
                                               .Replace("{orderId}", order.OrderCode)
                                               .Replace("{quantity}", order.ItemQuantity.ToString())
                                               .Replace("{totalOrder}", order.TotalAmount.ToString())
                                               .Replace("{orderDetail}", contentOrderDetail);
                }
                var roomNumber = _unitOfWork.Repository<Room>().GetAll().FirstOrDefault(x => x.Id == genOrder.RoomId).RoomNumber;
                body = body.Replace("{Total}", genOrder.TotalAmount.ToString())
                           .Replace("{discount}", genOrder.Discount.ToString())
                           .Replace("{shippingFee}", genOrder.ShippingFee.ToString())
                           .Replace("{finalTotal}", genOrder.FinalAmount.ToString())
                           .Replace("{customerName}", genOrder.Customer.Name)
                           .Replace("{phoneNumber}", genOrder.DeliveryPhone)
                           .Replace("{roomNumber}", roomNumber.ToString())
                           .Replace("{contentOrder}", contentOrder);

                return body;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> SendMailMessage(Order order)
        {
            try
            {
                int port;
                int.TryParse(_configuration["Mail:Port"], out port);
                SmtpClient smtpClient = new SmtpClient(_configuration["Mail:Host"], port);
                smtpClient.Credentials = new System.Net.NetworkCredential(_configuration["Mail:Username"], _configuration["Mail:Password"]);

                smtpClient.EnableSsl = true;
                smtpClient.UseDefaultCredentials = false;

                var customerInfo = _unitOfWork.Repository<Customer>().GetAll().FirstOrDefault(x => x.Id == order.CustomerId).Email; 
                MailMessage message = new MailMessage(_configuration["Mail:Username"],customerInfo);
                message.Subject = "Đơn hàng đây, đơn hàng đây! Fine mang niềm vui đến cho bạn nè!!!!";
                var mess = CreateMailMessage(order).Result;
                message.Body = mess;

                GeneratedBarcode Qrcode = IronBarCode.QRCodeWriter.CreateQrCode($"https://dev.fine-api.smjle.vn/api/order/orderStatus?orderId={order.Id}");
                Qrcode.AddAnnotationTextAboveBarcode("Scan me o((>ω< ))o");
                Qrcode.SaveAsPng("MyBarCode.png");
                var path = Path.Combine(Environment.CurrentDirectory + @"\MyBarCode.png");
                LinkedResource LinkedImage = new LinkedResource(path);
                LinkedImage.ContentId = "MyPic";

                AlternateView htmlView = AlternateView.CreateAlternateViewFromString(mess, null, "text/html");
                htmlView.LinkedResources.Add(LinkedImage);
                message.AlternateViews.Add(htmlView);  
                message.IsBodyHtml = true;

                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtpClient.EnableSsl = true;
                try
                {
                    smtpClient.Send(message);
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                    throw new Exception(ex.Message);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
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
                #region check timeslot
                var timeSlot = _unitOfWork.Repository<TimeSlot>().Find(x => x.Id == request.TimeSlotId);

                if (timeSlot == null)
                    throw new ErrorResponse(404, (int)TimeSlotErrorEnums.NOT_FOUND,
                        TimeSlotErrorEnums.NOT_FOUND.GetDisplayName());

                if (!Utils.CheckTimeSlot(timeSlot))
                    throw new ErrorResponse(400, (int)TimeSlotErrorEnums.OUT_OF_TIMESLOT,
                        TimeSlotErrorEnums.OUT_OF_TIMESLOT.GetDisplayName());
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

                    if (timeSlot.Menus.FirstOrDefault(x => x.Id == productInMenu.MenuId) == null)
                        throw new ErrorResponse(404, (int)MenuErrorEnums.NOT_FOUND_MENU_IN_TIMESLOT,
                           MenuErrorEnums.NOT_FOUND_MENU_IN_TIMESLOT.GetDisplayName());

                    if(productInMenu.IsAvailable == false ||productInMenu.Status != (int)ProductInMenuStatusEnum.Avaliable)
                        throw new ErrorResponse(404, (int)ProductInMenuErrorEnums.PRODUCT_NOT_AVALIABLE,
                       ProductInMenuErrorEnums.PRODUCT_NOT_AVALIABLE.GetDisplayName());

                    var detail = new PreOrderDetailRequest();
                    detail = _mapper.Map<PreOrderDetailRequest>(productInMenu);

                    detail.Quantity = orderDetail.Quantity;
                    detail.TotalAmount = (double)(detail.Price * detail.Quantity);
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
                                        orderCount.ToString().PadLeft(3, '0') + "." + Utils.GenerateRandomCode();

                //check phone number

                if (customer.Phone.Contains(request.DeliveryPhone) == null)
                {
                    if (!Utils.CheckVNPhone(request.DeliveryPhone))
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
                genOrder.CheckInDate = DateTime.Now;
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

        public async Task<BaseResponseViewModel<dynamic>> CreateOrder(int customerId, CreateGenOrderRequest request)
        {
            try
            {
                #region Timeslot
                var timeSlot = _unitOfWork.Repository<TimeSlot>().Find(x => x.Id == request.TimeSlotId);

                if (timeSlot == null)
                    throw new ErrorResponse(404, (int)TimeSlotErrorEnums.NOT_FOUND,
                        TimeSlotErrorEnums.NOT_FOUND.GetDisplayName());

                if (!Utils.CheckTimeSlot(timeSlot))
                    throw new ErrorResponse(400, (int)TimeSlotErrorEnums.OUT_OF_TIMESLOT,
                        TimeSlotErrorEnums.OUT_OF_TIMESLOT.GetDisplayName());
                #endregion

                #region Customer + delivery phone
                var customer = await _unitOfWork.Repository<Customer>().GetById(customerId);

                if (customer.Phone.Contains(request.DeliveryPhone) == null)
                {
                    if (!Utils.CheckVNPhone(request.DeliveryPhone))
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
                genOrder.OrderStatus = (int)OrderStatusEnum.PaymentPending;
                genOrder.IsConfirm = false;
                genOrder.IsPartyMode = false;

                foreach (var order in request.InverseGeneralOrder)
                {
                    foreach(var orderDetail in order.OrderDetails)
                    {
                        var productInMenu = _unitOfWork.Repository<ProductInMenu>().GetAll()
                                                       .Include(x => x.Menu)
                                                       .Include(x => x.Product)
                                                       .Where(x => x.Id == orderDetail.ProductInMenuId)
                                                       .FirstOrDefault();

                        if (timeSlot.Menus.FirstOrDefault(x => x.Id == productInMenu.MenuId) == null)
                            throw new ErrorResponse(404, (int)MenuErrorEnums.NOT_FOUND_MENU_IN_TIMESLOT,
                               MenuErrorEnums.NOT_FOUND_MENU_IN_TIMESLOT.GetDisplayName());

                        if (productInMenu.IsAvailable == false || productInMenu.Status != (int)ProductInMenuStatusEnum.Avaliable)
                            throw new ErrorResponse(400, (int)ProductInMenuErrorEnums.PRODUCT_NOT_AVALIABLE,
                           ProductInMenuErrorEnums.PRODUCT_NOT_AVALIABLE.GetDisplayName());
                    }
                    var inverseOrder = _mapper.Map<Order>(genOrder);
                    inverseOrder = _mapper.Map<CreateOrderRequest, Order>(order, inverseOrder);
                    genOrder.InverseGeneralOrder.Add(inverseOrder);
                }

                var payment = _paymentService.PreparePayment(request.PaymentType, request.PaymentAppType, genOrder);
                if (payment.IsFaulted == null)
                    throw new ErrorResponse(400, (int)PaymentErrorEnums.PAYMENT_FAIL,
                                            PaymentErrorEnums.PAYMENT_FAIL.GetDisplayName());
                genOrder.Payments = new List<Payment>();
                genOrder.Payments.Add(payment.Result);

                await _unitOfWork.Repository<Order>().InsertAsync(genOrder);
                await _unitOfWork.CommitAsync();

                try
                {
                    // create Notification for customer
                    var notifyRequest = new NotifyRequestModel
                    {
                        CustomerId = customer.Id,
                        CustomerName = customer.Name,
                        OrderCode = genOrder.OrderCode,
                        OrderStatus = genOrder.OrderStatus,
                        Type = (int)NotifyTypeEnum.ForOrder
                    };                    
                    BackgroundJob.Enqueue(() => _notifyService.CreateOrderNotify(notifyRequest));

                    BackgroundJob.Enqueue(() => SendMailMessage(genOrder));

                }catch(Exception ex)
                {
                    throw ex;
                }
                return new BaseResponseViewModel<dynamic>()
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

        public async Task CancelOrder(int orderId)
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
                }
                else
                {
                    foreach (var inverseOrder in order.InverseGeneralOrder)
                    {
                        inverseOrder.OrderStatus = (int)OrderStatusEnum.UserCancel;
                    }
                }
                order.OrderStatus = (int)OrderStatusEnum.UserCancel;

                await _unitOfWork.Repository<Order>().UpdateDetached(order);
                await _unitOfWork.CommitAsync();
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<BaseResponseViewModel<dynamic>> UpdateOrder(int orderId, UpdateOrderTypeEnum orderStatus, UpdateOrderRequest request)
        {
            try
            {
                var order = await _unitOfWork.Repository<Order>().GetAll()
                    .FirstOrDefaultAsync(x => x.Id == orderId);

                if (order == null)
                    throw new ErrorResponse(404, (int)OrderErrorEnums.NOT_FOUND_ID,
                              OrderErrorEnums.NOT_FOUND_ID.GetDisplayName());

                switch (orderStatus)
                {
                    case UpdateOrderTypeEnum.UserCancel:

                        if (order.OrderStatus != (int)OrderStatusEnum.PaymentPending
                            || order.OrderStatus != (int)OrderStatusEnum.Processing)
                            throw new ErrorResponse(400, (int)OrderErrorEnums.CANNOT_CANCEL_ORDER,
                              OrderErrorEnums.CANNOT_CANCEL_ORDER.GetDisplayName());

                         CancelOrder(orderId);
                        break;

                    case UpdateOrderTypeEnum.FinishOrder:

                        if (order.OrderStatus != (int)OrderStatusEnum.Delivering)
                            throw new ErrorResponse(400, (int)OrderErrorEnums.CANNOT_FINISH_ORDER,
                              OrderErrorEnums.CANNOT_FINISH_ORDER.GetDisplayName());

                        order.OrderStatus = (int)OrderStatusEnum.Finished;

                        await _unitOfWork.Repository<Order>().UpdateDetached(order);
                        await _unitOfWork.CommitAsync();
                        break;

                    case UpdateOrderTypeEnum.UpdateDetails:

                        if (order.OrderStatus == (int)OrderStatusEnum.Finished)
                            throw new ErrorResponse(400, (int)OrderErrorEnums.CANNOT_CANCEL_ORDER,
                              OrderErrorEnums.CANNOT_CANCEL_ORDER.GetDisplayName());

                        if(request.DeliveryPhone != null && !Utils.CheckVNPhone(request.DeliveryPhone))
                                throw new ErrorResponse(400, (int)OrderErrorEnums.INVALID_PHONE_NUMBER,
                                                        OrderErrorEnums.INVALID_PHONE_NUMBER.GetDisplayName());
                        
                        var updateOrder = _mapper.Map<UpdateOrderRequest, Order>(request, order);
                        updateOrder.UpdateAt = DateTime.Now;
                        await _unitOfWork.Repository<Order>().UpdateDetached(updateOrder);
                        break;
                }

                return new BaseResponseViewModel<dynamic>()
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

        public async Task<BaseResponseViewModel<dynamic>> CreateCoOrder(int customerId, CreateGenOrderRequest request)
        {
            try
            {
                #region Timeslot
                var timeSlot = _unitOfWork.Repository<TimeSlot>().Find(x => x.Id == request.TimeSlotId);

                if (timeSlot == null)
                    throw new ErrorResponse(404, (int)TimeSlotErrorEnums.NOT_FOUND,
                        TimeSlotErrorEnums.NOT_FOUND.GetDisplayName());

                if (!Utils.CheckTimeSlot(timeSlot))
                    throw new ErrorResponse(400, (int)TimeSlotErrorEnums.OUT_OF_TIMESLOT,
                        TimeSlotErrorEnums.OUT_OF_TIMESLOT.GetDisplayName());
                #endregion

                #region Customer
                var customer = await _unitOfWork.Repository<Customer>().GetById(customerId);

                //check phone number
                if (customer.Phone.Contains(request.DeliveryPhone) == null)
                {
                    if (!Utils.CheckVNPhone(request.DeliveryPhone))
                        throw new ErrorResponse(400, (int)OrderErrorEnums.INVALID_PHONE_NUMBER,
                                                OrderErrorEnums.INVALID_PHONE_NUMBER.GetDisplayName());
                }
                if (!request.DeliveryPhone.StartsWith("+84"))
                {
                    request.DeliveryPhone = request.DeliveryPhone.TrimStart(new char[] { '0' });
                    request.DeliveryPhone = "+84" + request.DeliveryPhone;
                }
                #endregion

                return null;
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}

