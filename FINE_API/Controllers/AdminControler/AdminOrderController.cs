using FINE.Service.DTO.Request;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace FINE.API.Controllers.AdminController;

[Route(Helpers.SettingVersionApi.ApiAdminVersion + "/order")]
[ApiController]
public class AdminOrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    public AdminOrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Get List Order Detail
    /// </summary>
    [Authorize(Roles = "SystemAdmin")]
    [HttpGet]
    public async Task<ActionResult<BaseResponsePagingViewModel<OrderDetailResponse>>> GetAllOrderDetail([FromQuery] OrderDetailResponse request, [FromQuery] PagingRequest paging)
    {
        return await _orderService.GetAllOrderDetail(request, paging);
    }
}