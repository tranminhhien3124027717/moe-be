using MOE_System.Application.DTOs.TopUp.Request;
using MOE_System.Application.DTOs.TopUp.Response;
using MOE_System.Domain.Entities;

namespace MOE_System.Application.Interfaces.Services;

public interface ITopUpService
{
    Task CreateScheduledTopUpAsync(CreateScheduledTopUpRequest request, CancellationToken cancellationToken);
    Task ProcessTopUpExecutionAsync(string ruleId, CancellationToken cancellationToken);
    Task<PaginatedTopUpScheduleResponse> GetTopUpSchedulesAsync(GetTopUpSchedulesRequest request, CancellationToken cancellationToken);
    Task<List<FilteredAccountHolderResponse>> GetSingaporeCitizenAccountHoldersAsync(string? search, CancellationToken cancellationToken);
    Task<List<FilteredAccountHolderResponse>> GetFilteredAccountHoldersAsync(GetFilteredAccountHoldersRequest request, CancellationToken cancellationToken);
    Task<TopupRuleDetailResponse> GetTopupRuleDetailAsync(string ruleId, string? educationAccountId, CancellationToken cancellationToken);
    Task<BatchRuleAffectedAccountsResponse> GetBatchRuleAffectedAccountsAsync(string ruleId, GetBatchRuleAffectedAccountsRequest request, CancellationToken cancellationToken);
    Task<TopupCustomizeFilterResponse> GetTopuCustomizeFilterAsync(CancellationToken cancellationToken);
    Task<CancelScheduledTopUpResponse> CancelScheduledTopUpAsync(string ruleId, CancelScheduledTopUpRequest request, CancellationToken cancellationToken);
}