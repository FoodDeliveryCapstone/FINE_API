using FINE.Service.DTO.Request;
using FINE.Service.DTO.Response;
using FINE.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Reso.Core.Custom;

namespace FINE.API.Controllers.AdminControler
{
    [Route(Helpers.SettingVersionApi.ApiAdminVersion + "/transaction")]
    [ApiController]
    public class AdminTransactionController : Controller
    {
        private readonly ITransactionService _transactionService;
        public AdminTransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }


        /// <summary>
        /// Get all Transaction
        /// </summary>
        [Authorize(Roles = "SystemAdmin, StoreManager")]
        [HttpGet]
        public async Task<ActionResult<BaseResponsePagingViewModel<TransactionResponse>>> GetAllTransaction([FromQuery] PagingRequest paging)
        {
            try
            {
                return await _transactionService.GetAllTransaction(paging);
            }
            catch (ErrorResponse ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
