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

namespace FINE.Service.Service
{
    public interface IQrCodeService
    {
        Task<dynamic> GenerateQrCode(string customerId, string boxId);
        Task<dynamic> GenerateShipperQrCode(BoxRequest request);
    }

    public class QrCodeService : IQrCodeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public QrCodeService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
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
                string content = box.Key + "." + Utils.GenerateRandomCode(10) + "." + boxId;

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

        public async Task<dynamic> GenerateShipperQrCode(BoxRequest request)
        {
            try
            {
                var listBox = await _unitOfWork.Repository<OrderBox>().GetAll().ToListAsync();
                listBox = listBox.Where(x => request.ListBoxId.Contains(x.BoxId)).ToList();

                var key = listBox.FirstOrDefault().Key;
                
                //foreach (var item in listBox)
                //{
                //    if(item.Key != key)                  
                //        throw new ErrorResponse(400, (int)BoxErrorEnums.BOX_NOT_AVAILABLE,
                //            BoxErrorEnums.BOX_NOT_AVAILABLE.GetDisplayName());                 
                //}

                foreach (var orderBox in listBox)
                {
                    if (orderBox.Status == (int)OrderBoxStatusEnum.Picked)
                        throw new ErrorResponse(400, (int)BoxErrorEnums.ORDER_TAKEN,
                            BoxErrorEnums.ORDER_TAKEN.GetDisplayName());

                    else if (orderBox.Status == (int)OrderBoxStatusEnum.StaffPicked)
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
                //string content = Utils.GenerateRandomCode(10) + ".";
                string content = "DRIVER" + "." +key.ToUpper() + "." + Utils.GenerateRandomCode(10) + ".";

                foreach (var boxId in request.ListBoxId)
                {
                    if(boxId == request.ListBoxId.LastOrDefault())
                    {
                        content += boxId.ToString().ToUpper();
                    }
                    else 
                    {
                        content += boxId.ToString().ToUpper() + ","; 
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
