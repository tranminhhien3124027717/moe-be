using MOE_System.Domain.Enums;

namespace MOE_System.Application.DTOs.TopUp.Request;

public sealed record GetTopUpSchedulesRequest
{
    public string? Search { get; init; }
    public List<RuleTargetType>? Types { get; init; }
    public List<TopUpStatus>? Statuses { get; init; }
    public DateTime? ScheduledDateFrom { get; init; }
    public DateTime? ScheduledDateTo { get; init; }
    public string? SortBy { get; init; } // type, name, amount, status, scheduledDate
    public bool SortDescending { get; init; } = false;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
