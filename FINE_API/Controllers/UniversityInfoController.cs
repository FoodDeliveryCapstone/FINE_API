using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.UniversityInfo;
using FINE.Service.DTO.Response;
using FINE.Service.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiVersion)]
    [ApiController]
    public class UniversityInfoController : ControllerBase
    {
        private readonly IUniversityInfoService _universityInfoService;

        public UniversityInfoController(IUniversityInfoService universityInfoService)
        {
            _universityInfoService = universityInfoService;
        }

        /// <summary>
        /// Get List University Info
        /// </summary>
        /// 
        [HttpGet]
        public async Task<ActionResult<BaseResponsePagingViewModel<UniversityInfoResponse>>> GetUniversityInfo
            ([FromQuery] UniversityInfoResponse request, [FromQuery] PagingRequest paging)
        {
            return await _universityInfoService.GetUniversityInformations(request, paging);
        }

        /// <summary>
        /// Get University Info By Id
        /// </summary>
        /// 
        [HttpGet("{universityInfoId}")]
        public async Task<ActionResult<BaseResponseViewModel<UniversityInfoResponse>>> GetUniversityInfoById
            ([FromRoute] int universityInfoId)
        {
            return await _universityInfoService.GetUniversityInfoById(universityInfoId);    
        }

        /// <summary>
        /// Create University Info 
        /// </summary>
        /// 
        [HttpPost]
        public async Task<ActionResult<BaseResponseViewModel<UniversityInfoResponse>>> CreateUniversity
            ([FromBody] CreateUniversityInfoRequest request)
        {
            return await _universityInfoService.CreateUniversityInfo(request);
        }

        /// <summary>
        /// Update University Info 
        /// </summary>
        /// 
        [HttpPut("{universityInfoId}")]
        public async Task<ActionResult<BaseResponseViewModel<UniversityInfoResponse>>> UpdateUniversityInfo
            ([FromRoute] int universityInfoId, [FromBody] UpdateUniversityInfoRequest request)
        {
            return await _universityInfoService.UpdateUniversityInfo(universityInfoId, request);
        }
    }
}
