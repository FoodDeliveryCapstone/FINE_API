using AutoMapper;
using FINE.Service.DTO.Request.Order;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FINE.Data.UnitOfWork;
using FINE.Service.Helpers;
using static FINE.Service.Helpers.Enum;
using static FINE.Service.Helpers.ErrorEnum;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using FINE.Service.DTO.Request.Shipper;
using Azure;
using FINE.Data.Entity;
using Microsoft.EntityFrameworkCore;

namespace FINE.Service.Service
{
    public interface IShipperService
    {
        Task<BaseResponseViewModel<ReportMissingProductResponse>> ReportMissingProductForShipper(ReportMissingProductRequest request);

    }

    public class ShipperService : IShipperService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IStaffService _staffService;
        private readonly IBoxService _boxService;
        public ShipperService(IUnitOfWork unitOfWork, IMapper mapper, IStaffService staffService, IBoxService boxService)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _staffService = staffService;
            _boxService = boxService;
        }

        public async Task<BaseResponseViewModel<ReportMissingProductResponse>> ReportMissingProductForShipper(ReportMissingProductRequest request)
        {
            try
            {
                #region check valid request
                var checkTimeslot = await _unitOfWork.Repository<TimeSlot>().GetAll().FirstOrDefaultAsync(x => x.Id == request.TimeSlotId);
                if(checkTimeslot == null)
                    throw new ErrorResponse(404, (int)TimeSlotErrorEnums.NOT_FOUND,
                                        TimeSlotErrorEnums.NOT_FOUND.GetDisplayName());
                var checkStation = await _unitOfWork.Repository<Station>().GetAll().FirstOrDefaultAsync(x => x.Id == request.StationId);
                if (checkStation == null)
                    throw new ErrorResponse(404, (int)StationErrorEnums.NOT_FOUND,
                                        StationErrorEnums.NOT_FOUND.GetDisplayName());
                var checkStore = await _unitOfWork.Repository<Store>().GetAll().FirstOrDefaultAsync(x => x.Id == request.StoreId);
                if (checkStore == null)
                    throw new ErrorResponse(404, (int)StoreErrorEnums.NOT_FOUND,
                                        StoreErrorEnums.NOT_FOUND.GetDisplayName());
                var checkBox = await _unitOfWork.Repository<Box>().GetAll().FirstOrDefaultAsync(x => x.Id == request.BoxId);
                if (checkBox == null)
                    throw new ErrorResponse(404, (int)BoxErrorEnums.NOT_FOUND,
                                        BoxErrorEnums.NOT_FOUND.GetDisplayName());
                #endregion

                var reportMissing = new ReportMissingProductResponse()
                {
                    ReportId = Guid.NewGuid(),
                    TimeSlotId = request.TimeSlotId,
                    StationId = request.StationId,
                    StoreId = request.StoreId,
                    BoxId = request.BoxId,
                    MissingProducts = request.MissingProducts,                      
                };
                ServiceHelpers.GetSetDataRedisReportMissingProduct(RedisSetUpType.SET, reportMissing.ReportId.ToString(), reportMissing);
                
                return new BaseResponseViewModel<ReportMissingProductResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0,
                    }
                };
            }
            catch (ErrorResponse ex)
            {
                throw ex;
            }
        }
    }
}
