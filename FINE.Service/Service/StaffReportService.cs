using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Commons;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Area;
using FINE.Service.DTO.Request.StaffReport;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using NetTopologySuite.Algorithm;
using NTQ.Sdk.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IStaffReportService
    {
        Task<BaseResponsePagingViewModel<StaffReportResponse>> GetStaffReports(StaffReportResponse filter, PagingRequest paging);
        Task<BaseResponseViewModel<StaffReportResponse>> GetStaffReportById(int staffReportId);
        Task<BaseResponseViewModel<StaffReportResponse>> CreateStaffReport(CreateStaffReport request);
        Task<BaseResponseViewModel<StaffReportResponse>> UpdateStaffReport(int staffReportId, UpdateStaffReport request);
    }

    public class StaffReportService : IStaffReportService
    {
        private IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public StaffReportService(IMapper mapepr, IUnitOfWork unitOfWork)
        {
            _mapper = mapepr;
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponseViewModel<StaffReportResponse>> CreateStaffReport(CreateStaffReport request)
        {
            var staffReportCheck = _unitOfWork.Repository<StaffReport>().Find(x => x.Id == request.StaffId);
            if (staffReportCheck != null)
            {
                throw new ErrorResponse(400, (int)StaffReportErrorEnum.STAFF_REPORT_EXSIST,
                                    StaffReportErrorEnum.STAFF_REPORT_EXSIST.GetDisplayName());
            }
            var staffReport = _mapper.Map<CreateStaffReport, StaffReport>(request);
            staffReport.CreateAt = DateTime.Now;
            await _unitOfWork.Repository<StaffReport>().InsertAsync(staffReport);
            await _unitOfWork.CommitAsync();

            return new BaseResponseViewModel<StaffReportResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<StaffReportResponse>(staffReport)
            };
        }

        public async Task<BaseResponseViewModel<StaffReportResponse>> GetStaffReportById(int staffReportId)
        {
            var staffReport = _unitOfWork.Repository<StaffReport>().GetAll()
                                    .FirstOrDefault(x => x.Id == staffReportId);

            return new BaseResponseViewModel<StaffReportResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<StaffReportResponse>(staffReport)
            };
        }

        public async Task<BaseResponsePagingViewModel<StaffReportResponse>> GetStaffReports(StaffReportResponse filter, PagingRequest paging)
        {
            var staff = _unitOfWork.Repository<StaffReport>().GetAll()
                                    .ProjectTo<StaffReportResponse>(_mapper.ConfigurationProvider)
                                       .DynamicFilter(filter)
                                       .DynamicSort(filter)
                                       .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging,
                                    Constants.DefaultPaging);
            return new BaseResponsePagingViewModel<StaffReportResponse>()
            {
                Metadata = new PagingsMetadata()
                {
                    Page = paging.Page,
                    Size = paging.PageSize,
                    Total = staff.Item1
                },
                Data = staff.Item2.ToList()
            };
        }

        public async Task<BaseResponseViewModel<StaffReportResponse>> UpdateStaffReport(int staffReportId, UpdateStaffReport request)
        {
            var staffReport = _unitOfWork.Repository<StaffReport>().Find(x => x.Id == staffReportId);
            if (staffReport == null)
            {
                throw new ErrorResponse(400, (int)StaffReportErrorEnum.NOT_FOUND_ID,
                                    StaffReportErrorEnum.NOT_FOUND_ID.GetDisplayName());
            }
            var staffReportMappingResult = _mapper.Map<UpdateStaffReport, StaffReport>(request, staffReport);
            staffReportMappingResult.UpdateAt = DateTime.Now;
            await _unitOfWork.Repository<StaffReport>()
                            .UpdateDetached(staffReportMappingResult);
            await _unitOfWork.CommitAsync();
            return new BaseResponseViewModel<StaffReportResponse>()
            {
                Status = new StatusViewModel()
                {
                    Message = "Success",
                    Success = true,
                    ErrorCode = 0
                },
                Data = _mapper.Map<StaffReportResponse>(staffReportMappingResult)
            };
        }
    }
}
