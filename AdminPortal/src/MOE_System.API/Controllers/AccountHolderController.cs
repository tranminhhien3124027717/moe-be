using Microsoft.AspNetCore.Mvc;
using MOE_System.Application.Interfaces;
using MOE_System.Application.Common;
using MOE_System.Application.DTOs.AccountHolder.Request;
using MOE_System.Application.DTOs.AccountHolder.Response;

namespace MOE_System.API.Controllers
{
    [Route("api/v1/admin/account-holders")]
    public class AccountHolderController : BaseApiController
    {
        private readonly IAccountHolderService _accountHolderService;
        
        public AccountHolderController(IAccountHolderService accountHolderService)
        {
            _accountHolderService = accountHolderService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<AccountHolderResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<AccountHolderResponse>>> CreateAccountHolder(CreateAccountHolderRequest request)
        {
            var newAccountHolder = await _accountHolderService.AddAccountHolderAsync(request);
            return Success(newAccountHolder, "Account holder created successfully");
        }

        [HttpPut]
        public async Task<ActionResult<ApiResponse>> UpdateAccountHolder(EditAccountHolderRequest request)
        {
            await _accountHolderService.UpdateAccountHolderAsync(request);
            return Success("Account holder updated successfully");
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PaginatedList<AccountHolderResponse>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<PaginatedList<AccountHolderResponse>>>> GetAccountHolders(
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 20,
            [FromQuery] AccountHolderFilterParams? filters = null)
        {
            var accountHolders = await _accountHolderService.GetAccountHoldersAsync(pageNumber, pageSize, filters);
            return Paginated(accountHolders.Items, pageNumber, pageSize, accountHolders.TotalCount, "Account holders retrieved successfully");
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<AccountHolderDetailResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<AccountHolderDetailResponse>>> GetAccountHolderDetail(string id)
        {
            var accountHolder = await _accountHolderService.GetAccountHolderDetailAsync(id);
            return Success(accountHolder, "Account holder details retrieved successfully");
        }
        
        [HttpGet("{id}/enrolled-courses")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedList<EnrolledCourseInfo>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<PaginatedList<EnrolledCourseInfo>>>> GetEnrolledCourses(
            string id,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 5)
        {
            var enrolledCourses = await _accountHolderService.GetEnrolledCoursesAsync(id, pageNumber, pageSize);
            return Paginated(enrolledCourses.Items, pageNumber, pageSize, enrolledCourses.TotalCount, "Enrolled courses retrieved successfully");
        }

        [HttpGet("{id}/outstanding-fees")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedList<OutstandingFeeInfo>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<PaginatedList<OutstandingFeeInfo>>>> GetOutstandingFees(
            string id,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 5)
        {
            var outstandingFees = await _accountHolderService.GetOutstandingFeesAsync(id, pageNumber, pageSize);
            return Paginated(outstandingFees.Items, pageNumber, pageSize, outstandingFees.TotalCount, "Outstanding fees retrieved successfully");
        }

        [HttpGet("{id}/topup-history")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedList<TopUpHistoryInfo>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<PaginatedList<TopUpHistoryInfo>>>> GetTopUpHistory(
            string id,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 5)
        {
            var topUpHistory = await _accountHolderService.GetTopUpHistoryAsync(id, pageNumber, pageSize);
            return Paginated(topUpHistory.Items, pageNumber, pageSize, topUpHistory.TotalCount, "Top up history retrieved successfully");
        }

        [HttpGet("{id}/payment-history")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedList<PaymentHistoryInfo>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<PaginatedList<PaymentHistoryInfo>>>> GetPaymentHistory(
            string id,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 5)
        {
            var paymentHistory = await _accountHolderService.GetPaymentHistoryAsync(id, pageNumber, pageSize);
            return Paginated(paymentHistory.Items, pageNumber, pageSize, paymentHistory.TotalCount, "Payment history retrieved successfully");
        }

        [HttpGet("{accountHolderId}/courses/{courseId}")]
        [ProducesResponseType(typeof(ApiResponse<StudentCourseDetailResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<StudentCourseDetailResponse>>> GetStudentCourseDetail(string accountHolderId, string courseId)
        {
            var studentCourseDetail = await _accountHolderService.GetStudentCourseDetailAsync(accountHolderId, courseId);
            return Success(studentCourseDetail, "Student course details retrieved successfully");
        }

        [HttpGet("resident-info")]
        public async Task<ActionResult<ApiResponse<ResidentInfoResponse>>> GetResidentInfo([FromQuery] string nric)
        {
            var residentInfo = await _accountHolderService.GetResidentAccountHolderByNRICAsync(nric);
            return Success(residentInfo, "Resident info retrieved successfully");
        }

        [HttpPost("{accountHolderId}/activate")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse>> ActivateAccount(string accountHolderId)
        {
            await _accountHolderService.ActivateAccountAsync(accountHolderId);
            return Success("Account activated successfully");
        }

        [HttpPost("{accountHolderId}/deactivate")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse>> DeactivateAccount(string accountHolderId)
        {
            await _accountHolderService.DeactivateAccountAsync(accountHolderId);
            return Success("Account deactivated successfully");
        }

        [HttpPost("bulk-activate")]
        [ProducesResponseType(typeof(ApiResponse<BulkAccountOperationResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<BulkAccountOperationResponse>>> BulkActivateAccounts(BulkAccountOperationRequest request)
        {
            var result = await _accountHolderService.ActivateAccountsAsync(request);
            return Success(result, "Bulk account activation completed");
        }

        [HttpPost("bulk-deactivate")]
        [ProducesResponseType(typeof(ApiResponse<BulkAccountOperationResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<BulkAccountOperationResponse>>> BulkDeactivateAccounts(BulkAccountOperationRequest request)
        {
            var result = await _accountHolderService.DeactivateAccountsAsync(request);
            return Success(result, "Bulk account deactivation completed");
        }

    }
}
