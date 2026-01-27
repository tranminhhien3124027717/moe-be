using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MOE_System.EService.Application.Common;
using MOE_System.EService.Application.DTOs;
using MOE_System.EService.Application.DTOs.AccountHolder;
using MOE_System.EService.Application.Interfaces.Services;
using System.Security.Claims;
using MOE_System.EService.Application.DTOs.EducationAccount;

namespace MOE_System.EService.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/account-holders")]
public class AccountHolderController : BaseApiController
{
    private readonly IAccountHolderService _accountHolderService;

    public AccountHolderController(IAccountHolderService accountHolderService)
    {
        _accountHolderService = accountHolderService;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(AccountHolderProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountHolderProfileResponse>> GetMyProfile()
    {
        // Get the account holder ID from JWT token claims
        var accountHolderId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(accountHolderId))
        {
            return Unauthorized("Invalid or missing authentication token");
        }

        var profile = await _accountHolderService.GetMyProfileAsync(accountHolderId);
        return Ok(profile);
    }

    [HttpPut("me")]
    public async Task<ActionResult<UpdateProfileResponse>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var accountHolderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(accountHolderId))
        {
            return Unauthorized("Invalid or missing authentication token");
        }
        var response = await _accountHolderService.UpdateProfileAsync(accountHolderId, request);
        return Ok(response);
    }

    [HttpPut("me/sync")]
    public async Task<ActionResult<ApiResponse>> SyncWithSingPass()
    {
        var accountHolderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(accountHolderId))
        {
            return Unauthorized("Invalid or missing authentication token");
        }
        await _accountHolderService.SyncProfileAsync(accountHolderId);
        return Success();
    }
    [HttpGet("{accountHolderId}")]
    [AllowAnonymous]
    public async Task<ActionResult<AccountHolderResponse>> GetAccountHolder([FromRoute] string accountHolderId)
    {
        var accountHolderResponse = await _accountHolderService.GetAccountHolderAsync(accountHolderId);
        return Ok(accountHolderResponse);
    }

    [HttpGet("courses/summary")]
    [ProducesResponseType(typeof(CourseSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<CourseSummaryResponse>>> GetCourseSummary()
    {
        var accountHolderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(accountHolderId))
        {
            return Unauthorized("Invalid or missing authentication token");
        }

        var summary = await _accountHolderService.GetCourseSummaryAsync(accountHolderId);
        return Success(summary, "Get course summary successfully");
    }

    [HttpGet("courses/enrolled")]
    [ProducesResponseType(typeof(EnrolledCoursesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<EnrolledCoursesResponse>>> GetEnrolledCourses(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var accountHolderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(accountHolderId))
        {
            return Unauthorized("Invalid or missing authentication token");
        }

        var request = new EnrolledCoursesRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var response = await _accountHolderService.GetEnrolledCoursesAsync(accountHolderId, request);
        return Success(response, "Get enrolled courses successfully");
    }

    [HttpGet("courses/pending-fees")]
    [ProducesResponseType(typeof(PendingFeesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PendingFeesResponse>>> GetPendingFees(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var accountHolderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(accountHolderId))
        {
            return Unauthorized("Invalid or missing authentication token");
        }

        var request = new PendingFeesRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var response = await _accountHolderService.GetPendingFeesAsync(accountHolderId, request);
        return Success(response, "Get pending fees successfully");
    }

    [HttpGet("courses/payment-history")]
    [ProducesResponseType(typeof(PaymentHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PaymentHistoryResponse>>> GetPaymentHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var accountHolderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(accountHolderId))
        {
            return Unauthorized("Invalid or missing authentication token");
        }

        var request = new PaymentHistoryRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var response = await _accountHolderService.GetPaymentHistoryAsync(accountHolderId, request);
        return Success(response, "Get payment history successfully");
    }

    [HttpGet("courses/{enrollmentId}/course-information")]
    [ProducesResponseType(typeof(CourseInformationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CourseInformationResponse>>> GetCourseInformation([FromRoute] string enrollmentId)
    {
        var accountHolderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(accountHolderId))
        {
            return Unauthorized("Invalid or missing authentication token");
        }

        var response = await _accountHolderService.GetCourseInformationAsync(accountHolderId, enrollmentId);
        return Success(response, "Get course information successfully");
    }

    [HttpGet("courses/{enrollmentId}/outstanding-fees")]
    [ProducesResponseType(typeof(OutstandingFeesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<OutstandingFeesResponse>>> GetOutstandingFees(
        [FromRoute] string enrollmentId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var accountHolderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(accountHolderId))
        {
            return Unauthorized("Invalid or missing authentication token");
        }

        var request = new OutstandingFeesRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var response = await _accountHolderService.GetOutstandingFeesAsync(accountHolderId, enrollmentId, request);
        return Success(response, "Get outstanding fees successfully");
    }

    [HttpGet("courses/{enrollmentId}/upcoming-billing-cycles")]
    [ProducesResponseType(typeof(UpcomingBillingCyclesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UpcomingBillingCyclesResponse>>> GetUpcomingBillingCycles(
        [FromRoute] string enrollmentId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var accountHolderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(accountHolderId))
        {
            return Unauthorized("Invalid or missing authentication token");
        }

        var request = new UpcomingBillingCyclesRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var response = await _accountHolderService.GetUpcomingBillingCyclesAsync(accountHolderId, enrollmentId, request);
        return Success(response, "Get upcoming billing cycles successfully");
    }


    [HttpGet("courses/{enrollmentId}/payment-history")]
    [ProducesResponseType(typeof(EnrollmentPaymentHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EnrollmentPaymentHistoryResponse>>> GetEnrollmentPaymentHistory(
        [FromRoute] string enrollmentId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var accountHolderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(accountHolderId))
        {
            return Unauthorized("Invalid or missing authentication token");
        }

        var request = new EnrollmentPaymentHistoryRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var response = await _accountHolderService.GetEnrollmentPaymentHistoryAsync(accountHolderId, enrollmentId, request);
        return Success(response, "Get payment history successfully");
    }

    [HttpGet("balance/transaction-history")]
    [ProducesResponseType(typeof(BalanceHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<BalanceHistoryResponse>>> GetTransactionHistory(
        [FromQuery] BalanceHistoryRequest request)
    {
        var accountHolderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(accountHolderId))
        {
            return Unauthorized("Invalid or missing authentication token");
        }

        var response = await _accountHolderService.GetTransactionHistoryAsync(accountHolderId, request);
        return Success(response, "Get transaction history successfully");
    }

    [HttpGet("balance")]
    [ProducesResponseType(typeof(BalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<BalanceResponse>>> GetBalance()
    {
        var accountHolderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(accountHolderId))
        {
            return Unauthorized("Invalid or missing authentication token");
        }
        var response = await _accountHolderService.GetBalanceAsync(accountHolderId);
        return Success(response, "Get account balance successfully");
    }
}
