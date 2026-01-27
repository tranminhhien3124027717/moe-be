using MOE_System.Domain.Enums;

namespace MOE_System.Application.DTOs.TopUp.Request;

public class CancelScheduledTopUpRequest
{
    public RuleTargetType Type { get; set; }
    public string? EducationAccountId { get; set; }
}