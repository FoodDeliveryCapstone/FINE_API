using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.Campus;
using FINE.Service.DTO.Request.Product_Collection_Time_Slot;
using FINE.Service.DTO.Response;
using FINE.Service.Service;
using Microsoft.AspNetCore.Mvc;
namespace FINE.API.Controllers
{
    [Route(Helpers.SettingVersionApi.ApiVersion)]
    [ApiController]
    public class ProductCollectionTimeSlotController : ControllerBase
    {
        private readonly IProductCollectionTimeSlotService _productCollectionTimeSlotService;

        public ProductCollectionTimeSlotController(IProductCollectionTimeSlotService productCollectionTimeSlotService)
        {
            _productCollectionTimeSlotService = productCollectionTimeSlotService;
        }

        /// <summary>
        /// Get All Product Collection Time Slot
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<BaseResponsePagingViewModel<ProductCollectionTimeSlotResponse>>> GetAllProductCollectionTimeSlot([FromQuery] ProductCollectionTimeSlotResponse request, [FromQuery] PagingRequest paging)
        {
            return await _productCollectionTimeSlotService.GetAllProductCollectionTimeSlot(request, paging);
        }

        /// <summary>
        /// Get Product Collection Time Slot By Id
        /// </summary>
        [HttpGet("{productCollectionTimeSlotId}")]
        public async Task<ActionResult<BaseResponseViewModel<ProductCollectionTimeSlotResponse>>> GetProductCollectionTimeSlotById([FromRoute] int productCollectionTimeSlotId)
        {
            return await _productCollectionTimeSlotService.GetProductCollectionTimeSlotById(productCollectionTimeSlotId);
        }

        /// <summary>
        /// Get All Product Collection Time Slot By ProductCollectionId
        /// </summary>
        [HttpGet("productCollection/{productCollectionId}")]
        public async Task<ActionResult<BaseResponsePagingViewModel<ProductCollectionTimeSlotResponse>>> GetProductCollectionTimeSlotByProductCollection([FromRoute] int productCollectionId, [FromQuery] PagingRequest paging)
        {
            return await _productCollectionTimeSlotService.GetProductCollectionTimeSlotByProductCollection(productCollectionId, paging);
        }

        /// <summary>
        /// Get All Product Collection Time Slot By TimeSlotId
        /// </summary>
        [HttpGet("timeSlot/{timeSlotId}")]
        public async Task<ActionResult<BaseResponsePagingViewModel<ProductCollectionTimeSlotResponse>>> GetProductCollectionTimeSlotByTimeSlot([FromRoute] int timeSlotId, [FromQuery] PagingRequest paging)
        {
            return await _productCollectionTimeSlotService.GetProductCollectionTimeSlotByTimeSlot(timeSlotId, paging);
        }

        /// <summary>
        /// Create Product Collection Time Slot
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<BaseResponseViewModel<ProductCollectionTimeSlotResponse>>> CreateProductCollectionTimeSlot([FromBody] CreateProductCollectionTimeSlotRequest request)
        {
            return await _productCollectionTimeSlotService.CreateProductCollectionTimeSlot(request);
        }


        /// <summary>
        /// Update Product Collection Time Slot
        /// </summary>
        [HttpPut("{productCollectionTimeSlotId}")]
        public async Task<ActionResult<BaseResponseViewModel<ProductCollectionTimeSlotResponse>>> UpdateProductCollectionTimeSlot([FromRoute] int productColletionTimeSlotId, [FromBody] UpdateProductCollectionTimeSlotRequest request)
        {
            return await _productCollectionTimeSlotService.UpdateProductCollectionTimeSlot(productColletionTimeSlotId, request);
        }
    }
}
