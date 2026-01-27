using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MOE_System.EService.Application.Common;
using MOE_System.EService.Application.DTOs;
using MOE_System.EService.Application.DTOs.EducationAccount;
using MOE_System.EService.Application.Interfaces.Services;


namespace MOE_System.EService.API.Controllers
{
    [Route("api/v1/education-accounts")]
    public class EducationAccountController : BaseApiController
    {
        private readonly IEducationAccountService _educationAccountService;

        public EducationAccountController(IEducationAccountService educationAccountService)
        {
            _educationAccountService = educationAccountService;
        }

        [HttpGet("{accountId}/outstanding-fees")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<OutstandingFeeResponse>>> GetAccountOutstandingFee([FromRoute] string accountId)
        {
            var outstandingFeeResponse = await _educationAccountService.GetOutstandingFeeAsync(accountId);

            return Success(outstandingFeeResponse);
        }

        /// <summary>
        /// Get transaction details including amount, type, description, date, time, and reference ID
        /// </summary>
        [HttpGet("{accountId}/transactions/{transactionId}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<TransactionDetailResponse>>> GetTransactionDetail(
            [FromRoute] string accountId, 
            [FromRoute] string transactionId)
        {
            var transactionDetail = await _educationAccountService.GetTransactionDetailAsync(accountId, transactionId);

            return Success(transactionDetail, "Transaction details retrieved successfully");
        }
    }
}

