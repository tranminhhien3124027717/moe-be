using Microsoft.AspNetCore.Mvc;
using MOE_System.Application.DTOs.TopUp.Request;
using MOE_System.Application.Interfaces.Services;

namespace MOE_System.API.Controllers;

[ApiController]
[Route("api/v1/admin/top-ups")]
public class TopupController : ControllerBase
{
    private readonly ITopUpService _topUpService;

    public TopupController(ITopUpService topUpService)
    {
        _topUpService = topUpService;
    }

    [HttpPost("scheduled")]
    public async Task<IActionResult> CreateScheduledTopUp([FromBody] CreateScheduledTopUpRequest request, CancellationToken cancellationToken)
    {
        await _topUpService.CreateScheduledTopUpAsync(request, cancellationToken);
        return Ok(new { Message = "Scheduled top-up created successfully." });
    }

    [HttpPost("scheduled/{ruleId}/cancel")]
    public async Task<IActionResult> CancelScheduledTopUp(
        [FromRoute] string ruleId,
        [FromBody] CancelScheduledTopUpRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _topUpService.CancelScheduledTopUpAsync(ruleId, request, cancellationToken);
        return Ok(result);
    }
    
    [HttpGet("schedules")]
    public async Task<IActionResult> GetTopUpSchedules([FromQuery] GetTopUpSchedulesRequest request, CancellationToken cancellationToken)
    {
        var schedules = await _topUpService.GetTopUpSchedulesAsync(request, cancellationToken);
        return Ok(schedules);
    }

    [HttpGet("account-holders/singapore-citizens")]
    public async Task<IActionResult> GetSingaporeCitizenAccountHolders([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var accountHolders = await _topUpService.GetSingaporeCitizenAccountHoldersAsync(search, cancellationToken);
        return Ok(accountHolders);
    }

    [HttpGet("account-holders/filtered")]
    public async Task<IActionResult> GetFilteredAccountHolders([FromQuery] GetFilteredAccountHoldersRequest request, CancellationToken cancellationToken)
    {
        var accountHolders = await _topUpService.GetFilteredAccountHoldersAsync(request, cancellationToken);
        return Ok(accountHolders);
    }

    [HttpGet("{ruleId}")]
    public async Task<IActionResult> GetTopupRuleDetail(
        [FromRoute] string ruleId,
        [FromQuery] string? educationAccountId,
        CancellationToken cancellationToken)
    {
        var ruleDetail = await _topUpService.GetTopupRuleDetailAsync(ruleId, educationAccountId, cancellationToken);
        return Ok(ruleDetail);
    }

    [HttpGet("{ruleId}/affected-accounts")]
    public async Task<IActionResult> GetBatchRuleAffectedAccounts(string ruleId, [FromQuery] GetBatchRuleAffectedAccountsRequest request, CancellationToken cancellationToken)
    {
        var affectedAccounts = await _topUpService.GetBatchRuleAffectedAccountsAsync(ruleId, request, cancellationToken);
        return Ok(affectedAccounts);
    }

    [HttpGet("customize-filters")]
    public async Task<IActionResult> GetTopupCustomizeFilter(CancellationToken cancellationToken)
    {
        var customizeFilters = await _topUpService.GetTopuCustomizeFilterAsync(cancellationToken);
        return Ok(customizeFilters);
    }
}