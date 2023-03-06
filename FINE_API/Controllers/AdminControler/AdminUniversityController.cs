using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.University;
using FINE.Service.DTO.Response;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiAdminVersion + "/university")]
    [ApiController]
    public class AdminUniversityController : ControllerBase
    {
        private readonly IUniversityService _universityService;

        public AdminUniversityController(IUniversityService universityService)
        {
            _universityService = universityService;
        }

        /// <summary>
        /// Get List Universities
        /// </summary>
        ///
        [Authorize(Roles = "SystemAdmin")]
        [HttpGet]
        public async Task<ActionResult<BaseResponsePagingViewModel<UniversityResponse>>> GetUniversities
            ([FromQuery] UniversityResponse filter, [FromQuery] PagingRequest request)
        {
            return await _universityService.GetUniversities(filter, request);
        }

        /// <summary>
        /// Get University By Id
        /// </summary>
        ///
        [Authorize(Roles = "SystemAdmin")]
        [HttpGet("{universityId}")]
        public async Task<ActionResult<BaseResponseViewModel<UniversityResponse>>> GetUniversityById
            ([FromRoute] int universityId)
        {
            return await _universityService.GetUniversityById(universityId);
        }

        /// <summary>
        /// Create University
        /// </summary>
        [Authorize(Roles = "SystemAdmin")]
        [HttpPost]
        public async Task<ActionResult<BaseResponseViewModel<UniversityResponse>>> CreateUniversity
            ([FromBody] CreateUniversityRequest request)
        {
            return await _universityService.CreateUniversity(request);
        }

        /// <summary>
        /// Update University 
        /// </summary>
        ///
        [Authorize(Roles = "SystemAdmin")]
        [HttpPut("{universityId}")]
        public async Task<ActionResult<BaseResponseViewModel<UniversityResponse>>> UpdateUniversity
            ([FromRoute] int universityId, [FromBody] UpdateUniversityRequest request)
        {
            return await _universityService.UpdateUnivesity(universityId, request);
        }
    }
}
