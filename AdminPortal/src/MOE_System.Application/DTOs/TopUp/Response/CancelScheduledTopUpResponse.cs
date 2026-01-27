using MOE_System.Domain.Enums;

namespace MOE_System.Application.DTOs.TopUp.Response;

public class CancelScheduledTopUpResponse
{
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public RuleTargetType Type { get; set; }
    public bool EntireRuleCancelled { get; set; }
    public int RemainingTargets { get; set; }
    public string Message { get; set; } = string.Empty;
}