using AutoMapper;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using IronBarCode;
using IronSoftware.Drawing;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Index.HPRtree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.Enum;
using static FINE.Service.Helpers.ErrorEnum;
using static IronSoftware.Drawing.AnyBitmap;

namespace FINE.Service.Service
{
    public interface IQrCodeService
    {
        Task<dynamic> GenerateQrCode(string customerId, string boxId);
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

                GeneratedBarcode qrCode = IronBarCode.QRCodeWriter.CreateQrCode(boxId);
                qrCode.AddAnnotationTextAboveBarcode("Scan me o((>ω< ))o");
                qrCode.ToImage();
                return qrCode;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
