using FINE.Service.Caches;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Destination;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiVersion)]
    [ApiController]
    public class DestinationController : ControllerBase
    {
        private readonly IDestinationService _destinationService;

        public DestinationController(IDestinationService destinationService)
        {
            _destinationService = destinationService;
        }

        /// <summary>
        /// Get Destination By Id
        /// </summary>
        [Cache(18000)]
        [HttpGet("{id}")]
        public async Task<ActionResult<BaseResponseViewModel<DestinationResponse>>> GetDestinationById([FromRoute] string id)
        {
            try
            {
                return await _destinationService.GetDestinationById(id);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Error);
            }
        }


    }
}
