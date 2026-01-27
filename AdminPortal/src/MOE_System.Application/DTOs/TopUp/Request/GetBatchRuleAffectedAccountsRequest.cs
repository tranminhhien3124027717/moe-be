namespace MOE_System.Application.DTOs.TopUp.Request;

public sealed record GetBatchRuleAffectedAccountsRequest
{
    public string? Search { get; init; }
}
