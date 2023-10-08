using AutoMapper;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using Microsoft.EntityFrameworkCore;
using ZXing.QrCode;
using ZXing;
using static FINE.Service.Helpers.Enum;
using static FINE.Service.Helpers.ErrorEnum;
using ZXing.Windows.Compatibility;
using System.Drawing;
using FINE.Service.DTO.Request.Box;
using NetTopologySuite.Index.HPRtree;
using FirebaseAdmin.Messaging;
using Hangfire;

namespace FINE.Service.Service
{
    public interface IQrCodeService
    {
        Task<dynamic> GenerateQrCode(string customerId, string boxId);
        Task<dynamic> GenerateShipperQrCode(List<AddOrderToBoxRequest> request);
        Task ReceiveBoxResult(string boxId, string key);
    }

    public class QrCodeService : IQrCodeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IFirebaseMessagingService _fm;
        private readonly IPaymentService _paymentService;
        public QrCodeService(IUnitOfWork unitOfWork, IMapper mapper, IFirebaseMessagingService fm, IPaymentService paymentService)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _fm = fm;
            _paymentService = paymentService;
        }


        public async Task<dynamic> GenerateQrCode(string customerId, string boxId)
        {
            try
            {
                var box = await _unitOfWork.Repository<OrderBox>().GetAll()
                                .Include(x => x.Order)
                                .Where(x => x.BoxId == Guid.Parse(boxId)
                                    && x.Order.CustomerId == Guid.Parse(customerId))
                                .FirstOrDefaultAsync();

                if (box.Status == (int)OrderBoxStatusEnum.Picked)
                    throw new ErrorResponse(400, (int)BoxErrorEnums.ORDER_TAKEN,
                        BoxErrorEnums.ORDER_TAKEN.GetDisplayName());

                else if (box.Status == (int)OrderBoxStatusEnum.StaffPicked)
                    throw new ErrorResponse(400, (int)BoxErrorEnums.STAFF_TAKEN,
                         BoxErrorEnums.STAFF_TAKEN.GetDisplayName());

                QrCodeEncodingOptions options = new()
                {
                    DisableECI = true,
                    CharacterSet = "UTF-8",
                    Width = 500,
                    Height = 500
                };
                string content = "1" + box.Key + "." + Utils.GenerateRandomCode(10) + "." + boxId;

                BarcodeWriter writer = new()
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = options
                };
                Bitmap qrCodeBitmap = writer.Write(content);

                return qrCodeBitmap;
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task ReceiveBoxResult(string boxId, string key)
        {
            try
            {
                var orderBox = _unitOfWork.Repository<OrderBox>().GetAll()
                                    .FirstOrDefault(x => x.BoxId == Guid.Parse(boxId) && x.Key.Contains(key));
                if (orderBox == null)
                    throw new ErrorResponse(400, (int)BoxErrorEnums.ORDER_BOX_ERROR, BoxErrorEnums.ORDER_BOX_ERROR.GetDisplayName());

                orderBox.Status = (int)OrderBoxStatusEnum.Picked;
                orderBox.UpdateAt = DateTime.Now;

                 _unitOfWork.Repository<OrderBox>().UpdateDetached(orderBox);

                var order = _unitOfWork.Repository<Order>().GetAll()
                    .FirstOrDefault(x => x.Id == orderBox.OrderId);
                order.OrderStatus = (int)OrderStatusEnum.Finished;
                order.UpdateAt = DateTime.Now;
                _unitOfWork.Repository<Order>().UpdateDetached(order);
                _unitOfWork.Commit();

                var token = _unitOfWork.Repository<Fcmtoken>().GetAll()
                                    .FirstOrDefault(x => x.UserId == orderBox.Order.CustomerId).Token;
                Notification notification = new Notification()
                {
                    Title = "Thành công ùi!!!",
                    Body = "Bạn đã mở box thành công!!! Sau khi lấy hàng nhớ đóng tủ giúp tụi mình nha!"
                };
                Dictionary<string, string> data = new Dictionary<string, string>()
                {
                    { "type", NotifyTypeEnum.ForPopup.ToString()}
                };
                BackgroundJob.Enqueue(() => _fm.SendToToken(token, notification, data));

                var party = orderBox.Order.Parties.FirstOrDefault(x => x.PartyType == (int)PartyOrderType.LinkedOrder);
                if (party is not null)
                {
                    _paymentService.RefundPartialLinkedFee(party.PartyCode, party.CustomerId);
                }
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }

        public async Task<dynamic> GenerateShipperQrCode(List<AddOrderToBoxRequest> request)
        {
            try
            {
                var listOrderBox = await _unitOfWork.Repository<OrderBox>().GetAll().ToListAsync();

                var key = listOrderBox.FirstOrDefault().Key;
                var newListOrderBox = new List<OrderBox>();

                foreach (var item in listOrderBox)
                {
                    if(request.FirstOrDefault(x => x.BoxId == item.BoxId && x.OrderId == item.OrderId) != null)
                    {
                        newListOrderBox.Add(item);
                        key = item.Key;
                    }                  
                }

                foreach (var orderBox in newListOrderBox)
                {
                    if (orderBox.Key != key)
                        throw new ErrorResponse(400, (int)BoxErrorEnums.BOX_NOT_AVAILABLE,
                            BoxErrorEnums.BOX_NOT_AVAILABLE.GetDisplayName());
                    if (orderBox.Status == (int)OrderBoxStatusEnum.Picked)
                        throw new ErrorResponse(400, (int)BoxErrorEnums.ORDER_TAKEN,
                            BoxErrorEnums.ORDER_TAKEN.GetDisplayName());
                    if (orderBox.Status == (int)OrderBoxStatusEnum.StaffPicked)
                        throw new ErrorResponse(400, (int)BoxErrorEnums.STAFF_TAKEN,
                             BoxErrorEnums.STAFF_TAKEN.GetDisplayName());
                   
                }
                QrCodeEncodingOptions options = new()
                {
                    DisableECI = true,
                    CharacterSet = "UTF-8",
                    Width = 500,
                    Height = 500
                };
                //Driver là 2, customer là 1
                string content = "2" + "." + key.ToUpper() + "." + Utils.GenerateRandomCode(10) + ".";

                foreach (var item in newListOrderBox)
                {
                    if(item == newListOrderBox.LastOrDefault())
                    {
                        content += item.BoxId.ToString().ToUpper();
                    }
                    else 
                    {
                        content += item.BoxId.ToString().ToUpper() + ","; 
                    }
                };

                BarcodeWriter writer = new()
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = options
                };
                Bitmap qrCodeBitmap = writer.Write(content);

                return qrCodeBitmap;

            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}
