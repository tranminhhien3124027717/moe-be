using MOE_System.Application.DTOs.Dashboard.Response;
using MOE_System.Domain.Enums;

namespace MOE_System.Application.Interfaces.Services;

public interface IDashboardService
{
    Task<IReadOnlyList<ScheduledTopUpResponse>> GetTopUpTypesAsync(RuleTargetType type, CancellationToken cancellationToken);
    Task<IReadOnlyList<RecentActivityResponse>> GetRecentActivitiesAsync(CancellationToken cancellationToken);
}