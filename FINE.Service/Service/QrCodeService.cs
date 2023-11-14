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
using FINE.Service.DTO.Response;
using FINE.Service.Helpers;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Linq;

namespace FINE.Service.Service
{
    public interface IQrCodeService
    {
        Task<dynamic> GenerateQrCode(string customerId, string orderId);
        Task<dynamic> GenerateShipperQrCode(string staffId, string timeSlotId);
        Task<BaseResponseViewModel<dynamic>> ReceiveBoxResult(string orderId);
        Task<BaseResponseViewModel<QROrderBoxResponse>> GetListBoxAndKey(string staffId, string timeSlotId);
    }

    public class QrCodeService : IQrCodeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IFirebaseMessagingService _fm;
        private readonly IPaymentService _paymentService;
        private readonly IConfiguration _configuration;
        public QrCodeService(IUnitOfWork unitOfWork, IMapper mapper, IFirebaseMessagingService fm, IPaymentService paymentService, IConfiguration configuration)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _fm = fm;
            _paymentService = paymentService;
            _configuration = configuration;
        }

        public async Task<dynamic> GenerateQrCode(string customerId, string orderId)
        {
            try
            {
                var box = await _unitOfWork.Repository<OrderBox>().GetAll()
                                .Include(x => x.Order)
                                .Where(x => x.OrderId == Guid.Parse(orderId)
                                    && x.Order.CustomerId == Guid.Parse(customerId))
                                .ToListAsync();

                if (box.Any(x => x.Status == (int)OrderBoxStatusEnum.Picked))
                    throw new ErrorResponse(400, (int)BoxErrorEnums.ORDER_TAKEN,
                        BoxErrorEnums.ORDER_TAKEN.GetDisplayName());

                else if (box.Any(x => x.Status == (int)OrderBoxStatusEnum.StaffPicked))
                    throw new ErrorResponse(400, (int)BoxErrorEnums.STAFF_TAKEN,
                         BoxErrorEnums.STAFF_TAKEN.GetDisplayName());

                QrCodeEncodingOptions options = new()
                {
                    DisableECI = true,
                    CharacterSet = "UTF-8",
                    Width = 500,
                    Height = 500
                };
                var content = (int)QRCodeRole.User + "." + orderId;

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
        public async Task<BaseResponseViewModel<dynamic>> ReceiveBoxResult(string orderId)
        {
            try
            {
                var orderBoxs = await _unitOfWork.Repository<OrderBox>().GetAll()
                                    .Where(x => x.OrderId == Guid.Parse(orderId)).ToListAsync();
                var order = await _unitOfWork.Repository<Order>().GetAll()
                                    .FirstOrDefaultAsync(x => x.Id == Guid.Parse(orderId));

                var party = order.Parties.FirstOrDefault(x => x.PartyType == (int)PartyOrderType.LinkedOrder);

                var token = _unitOfWork.Repository<Fcmtoken>().GetAll()
                                    .FirstOrDefault(x => x.UserId == order.CustomerId).Token;

                if (orderBoxs == null)
                    throw new ErrorResponse(400, (int)BoxErrorEnums.ORDER_BOX_ERROR, BoxErrorEnums.ORDER_BOX_ERROR.GetDisplayName());

                if (party is not null)
                {
                    _paymentService.RefundPartialLinkedFee(party.PartyCode, party.CustomerId);
                }
                var otherAmount = order.OtherAmounts.Where(x => x.Type == (int)OtherAmountTypeEnum.Refund).ToList();
                if (otherAmount is not null)
                {
                    _paymentService.RefundRefuseAmount(otherAmount);
                }
                foreach (var orderBox in orderBoxs)
                {
                    orderBox.Status = (int)OrderBoxStatusEnum.Picked;
                    orderBox.UpdateAt = DateTime.Now;

                    _unitOfWork.Repository<OrderBox>().UpdateDetached(orderBox);
                }

                order.OrderStatus = (int)OrderStatusEnum.Finished;
                order.UpdateAt = DateTime.Now;

                await _unitOfWork.Repository<Order>().UpdateDetached(order);
                await _unitOfWork.CommitAsync();

                Notification notification = new Notification()
                {
                    Title = "",
                    Body = ""
                };
                Dictionary<string, string> data = new Dictionary<string, string>()
                {
                    { "type", NotifyTypeEnum.ForFinishOrder.ToString()},
                    { "orderId", order.Id.ToString() }
                };

                BackgroundJob.Enqueue(() => _fm.SendToToken(token, notification, data));
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
        public async Task<dynamic> GenerateShipperQrCode(string staffId, string timeSlotId)
        {
            try
            {
                QrCodeEncodingOptions options = new()
                {
                    DisableECI = true,
                    CharacterSet = "UTF-8",
                    Width = 500,
                    Height = 500
                };

                var content = (int)QRCodeRole.Shipper + "." + staffId + "." + timeSlotId;

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
        public async Task<BaseResponseViewModel<QROrderBoxResponse>> GetListBoxAndKey(string staffId, string timeSlotId)
        {
            try
            {
                PackageShipperResponse packageShipperResponse = new PackageShipperResponse();
                QROrderBoxResponse result = new QROrderBoxResponse()
                {
                    Key = Utils.GenerateRandomCode(5),
                    ListBox = new List<Guid>()
                };

                var staff = await _unitOfWork.Repository<Staff>().GetAll().FirstOrDefaultAsync(x => x.Id == Guid.Parse(staffId));

                var timeSlot = await _unitOfWork.Repository<TimeSlot>().GetAll().FirstOrDefaultAsync(x => x.Id == Guid.Parse(timeSlotId));

                var key = RedisDbEnum.Shipper.GetDisplayName() + ":" + staff.Station.Code + ":" + timeSlot.ArriveTime.ToString(@"hh\-mm\-ss");

                var redisShipperValue = await ServiceHelpers.GetSetDataRedis(RedisSetUpType.GET, key, null);

                if (redisShipperValue.HasValue == true)
                {
                    packageShipperResponse = JsonConvert.DeserializeObject<PackageShipperResponse>(redisShipperValue);
                }
                HashSet<Guid> setOrderId = new HashSet<Guid>();

                setOrderId = setOrderId.Concat(packageShipperResponse.PackageStoreShipperResponses.SelectMany(x => x.ListOrderId).Where(x => x.Value == false).Select(x => x.Key)).ToHashSet();

                foreach (var id in setOrderId)
                {
                    var orderBox = _unitOfWork.Repository<OrderBox>().GetAll().Where(x => x.OrderId == id).AsQueryable();

                    foreach (var box in orderBox)
                    {
                        result.ListBox.Add(box.BoxId);
                    }
                    _unitOfWork.Repository<OrderBox>().UpdateRange(orderBox);
                }
                packageShipperResponse.PackageStoreShipperResponses.SelectMany(x => x.ListOrderId).Where(x => x.Value == false).Select(x => new KeyValuePair<Guid, bool>
                (
                    x.Key, true
                ));

                _unitOfWork.Commit();
                ServiceHelpers.GetSetDataRedis(RedisSetUpType.SET, key, packageShipperResponse);
                return new BaseResponseViewModel<QROrderBoxResponse>()
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
    }
}
