using MOE_System.Domain.Enums;

namespace MOE_System.Application.DTOs.TopUp.Response;

public sealed record TopUpScheduleResponse
{
    public string Id { get; init; } = string.Empty;
    public string RuleName { get; init; } = string.Empty;
    public RuleTargetType Type { get; init; }
    public decimal Amount { get; init; }
    public TopUpStatus Status { get; init; }
    public DateTime ScheduledTime { get; init; }
    public DateTime? ExecutedTime { get; init; }
    public DateTime CreatedDate { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public int? NumberOfAccountsAffected { get; init; }

    // Individual-specific - flat fields (one row per target)
    public string? TargetEducationAccountId { get; init; }
    public string? TargetAccountHolderName { get; init; }
    public string? TargetAccountHolderNric { get; init; }
}

public sealed record PaginatedTopUpScheduleResponse
{
    public List<TopUpScheduleResponse> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
}
