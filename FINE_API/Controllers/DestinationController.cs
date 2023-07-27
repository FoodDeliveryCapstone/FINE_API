//using FINE.Service.DTO.Request;
//using FINE.Service.DTO.Request.Destination;
//using FINE.Service.DTO.Response;
//using FINE.Service.Exceptions;
//using FINE.Service.Service;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;

//namespace FINE.API.Controllers
//{
//    [Route(Helpers.SettingVersionApi.ApiVersion)]
//    [ApiController]
//    public class DestinationController : ControllerBase
//    {
//        private readonly IDestinationService _destinationService;

//        public DestinationController(IDestinationService destinationService)
//        {
//            _destinationService = destinationService;
//        }

//        /// <summary>
//        /// Get List Destination
//        /// </summary>
//        [HttpGet]
//        public async Task<ActionResult<BaseResponsePagingViewModel<DestinationResponse>>> GetListDestination([FromQuery] DestinationResponse request, [FromQuery] PagingRequest paging)
//        {
//            try
//            {
//                return await _destinationService.GetListDestination(request, paging);

//            }
//            catch (Exception ex)
//            {
//                return BadRequest(ex.Message);
//            }
//        }

//        /// <summary>
//        /// Get Destination By Id
//        /// </summary>
//        [HttpGet("{id}")]
//        public async Task<ActionResult<BaseResponseViewModel<DestinationResponse>>> GetDestinationById([FromRoute] string id)
//        {
//            try
//            {
//                return await _destinationService.GetDestinationById(id);
//            }
//            catch (ErrorResponse ex)
//            {
//                return BadRequest(ex.Error);
//            }
//        }

   
//    }
//}
