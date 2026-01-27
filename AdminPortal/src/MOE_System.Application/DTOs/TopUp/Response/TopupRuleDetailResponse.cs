using MOE_System.Domain.Enums;

namespace MOE_System.Application.DTOs.TopUp.Response;

public sealed record TopupRuleDetailResponse
{
    // Common fields
    public string Id { get; init; } = string.Empty;
    public RuleTargetType Type { get; init; }
    public string? Description { get; init; }
    public string? InternalRemarks { get; init; }
    public decimal AmountPerAccount { get; init; }
    public TopUpStatus Status { get; init; }
    public DateTime ScheduledDate { get; init; }
    public DateTime? ExecutedTime { get; init; }

    // Individual-specific fields
    public string? AccountName { get; init; }
    public string? AccountId { get; init; }

    // Batch-specific fields
    public string? RuleName { get; init; }
    public int? EligibleAccounts { get; init; }
    public decimal? TotalDisbursement { get; init; }
    public TopupRuleCriteriaResponse? TopupRules { get; init; }
}

public sealed record TopupRuleCriteriaResponse
{
    public BatchFilterType TargetingType { get; init; }
    public int? MinAge { get; init; }
    public int? MaxAge { get; init; }
    public decimal? MinBalance { get; init; }
    public decimal? MaxBalance { get; init; }
    public List<string>? EducationLevels { get; init; }
    public List<string>? SchoolingStatuses { get; init; }
}
