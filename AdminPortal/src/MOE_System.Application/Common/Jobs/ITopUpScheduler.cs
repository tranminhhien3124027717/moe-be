using MOE_System.Domain.Entities;

namespace MOE_System.Application.Common.Jobs;

public interface ITopUpScheduler
{
    Task ScheduleTopupJobAsync(TopupRule rule);
    Task UnscheduleTopupJobAsync(string ruleId);
}