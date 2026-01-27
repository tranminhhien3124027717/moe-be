using MOE_System.Domain.Enums;

namespace MOE_System.Application.DTOs.TopUp.Request;

public sealed record CreateScheduledTopUpRequest
(
    string RuleName,
    decimal TopupAmount,
    RuleTargetType RuleTargetType,
    BatchFilterType? BatchFilterType,
    DateTime? ScheduledTime,

    bool ExecuteImmediately = false,
    
    ICollection<string>? TargetEducationAccountId = null,

    int? MinAge = null,
    int? MaxAge = null,
    decimal? MinBalance = null,
    decimal? MaxBalance = null,

    ICollection<string>? EducationLevelIds = null,
    ICollection<string>? SchoolingStatusIds = null,
    
    string? Description = null,
    string? InternalRemarks = null
);