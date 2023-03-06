using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.StaffReport;
using FINE.Service.DTO.Response;
using FINE.Service.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers.AdminControler
{
    [Route(Helpers.SettingVersionApi.ApiVersion)]
    [ApiController]
    public class StaffReportController : ControllerBase
    {
        private readonly IStaffReportService _staffReportService;

        public StaffReportController(IStaffReportService staffReportService)
        {
            _staffReportService = staffReportService;
        }

        /// <summary>
        /// Get List Staff Reports    
        /// </summary>
        /// 
        [HttpGet]
        public async Task<ActionResult<BaseResponsePagingViewModel<StaffReportResponse>>> GetStaffReports
            ([FromQuery] StaffReportResponse filter, [FromQuery] PagingRequest request)
        {
            return await _staffReportService.GetStaffReports(filter, request);
        }

        /// <summary>
        /// Get Staff Report By Id
        /// </summary>
        /// 
        [HttpGet("{staffReportId}")]
        public async Task<ActionResult<BaseResponseViewModel<StaffReportResponse>>> GetStaffReportById
            ([FromRoute] int staffReportId)
        {
            return await _staffReportService.GetStaffReportById(staffReportId);
        }

        /// <summary>
        /// Create Staff Report
        /// </summary>
        ///
        [HttpPost]
        public async Task<ActionResult<BaseResponseViewModel<StaffReportResponse>>> CreateStaffReport
            ([FromBody] CreateStaffReport request)
        {
            return await _staffReportService.CreateStaffReport(request);
        }

        /// <summary>
        /// Update Staff Report 
        /// </summary>
        ///
        [HttpPut("{staffReportId}")]
        public async Task<ActionResult<BaseResponseViewModel<StaffReportResponse>>> UpdateStaffReport
            ([FromRoute] int staffReportId, [FromBody] UpdateStaffReport request)
        {
            return await _staffReportService.UpdateStaffReport(staffReportId, request);
        }
    }
}
