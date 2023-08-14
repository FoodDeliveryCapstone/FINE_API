using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Attributes;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Box;
using FINE.Service.DTO.Request.Staff;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IBoxService
    {
        Task<BaseResponseViewModel<ScanBoxResponse>> ScanBoxBarcode(BarcodeRequest request);
    }

    public class BoxService : IBoxService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public BoxService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponseViewModel<ScanBoxResponse>> ScanBoxBarcode(BarcodeRequest request)
        {
            try
            {
                var barcode = _unitOfWork.Repository<Box>().GetAll()
                                .FirstOrDefault(x => x.Code == request.Code);
                if (barcode == null)
                    throw new ErrorResponse(404, (int)BoxErrorEnums.NOT_FOUND,
                       BoxErrorEnums.NOT_FOUND.GetDisplayName());

                return new BaseResponseViewModel<ScanBoxResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<ScanBoxResponse>(barcode)
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}