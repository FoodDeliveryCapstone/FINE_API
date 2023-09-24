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
using FirebaseAdmin.Messaging;
using Hangfire;

namespace FINE.Service.Service
{
    public interface IQrCodeService
    {
        Task<dynamic> GenerateQrCode(string customerId, string boxId);
        Task ReceiveBoxResult(string boxId, string key);
    }

    public class QrCodeService : IQrCodeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IFirebaseMessagingService _fm;
        public QrCodeService(IUnitOfWork unitOfWork, IMapper mapper, IFirebaseMessagingService fm)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _fm = fm;
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
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task ReceiveBoxResult(string boxId, string key)
        {
            try
            {
                var orderBox = await _unitOfWork.Repository<OrderBox>().GetAll()
                                    .FirstOrDefaultAsync(x => x.BoxId == Guid.Parse(boxId) && x.Key == key);
                if (orderBox == null)
                    throw new ErrorResponse(400, (int)BoxErrorEnums.ORDER_BOX_ERROR, BoxErrorEnums.ORDER_BOX_ERROR.GetDisplayName());

                orderBox.Status = (int)OrderBoxStatusEnum.Picked;
                orderBox.UpdateAt = DateTime.Now;   

                await _unitOfWork.Repository<OrderBox>().UpdateDetached(orderBox);
                await _unitOfWork.CommitAsync();

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
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}
