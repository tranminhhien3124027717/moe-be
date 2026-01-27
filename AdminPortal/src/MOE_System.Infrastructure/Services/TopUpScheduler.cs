using MOE_System.Application.Common.Jobs;
using MOE_System.Domain.Entities;
using MOE_System.Infrastructure.Jobs;
using Quartz;

namespace MOE_System.Infrastructure.Services;

public class TopUpScheduler : ITopUpScheduler
{
    private readonly ISchedulerFactory _schedulerFactory;

    public TopUpScheduler(ISchedulerFactory schedulerFactory)
    {
        _schedulerFactory = schedulerFactory;
    }

    public async Task ScheduleTopupJobAsync(TopupRule rule)
    {
        var scheduler = await _schedulerFactory.GetScheduler();

        var jobKey = new JobKey($"TopUpJob_{rule.Id}", "TopUpGroup");

        var job = JobBuilder.Create<TopUpExecutionJob>()
            .WithIdentity(jobKey)
            .UsingJobData("RuleId", rule.Id)
            .StoreDurably()
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"TopUpTrigger_{rule.Id}", "TopUpGroup")
            .StartAt(new DateTimeOffset(rule.ScheduledTime))
            .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
            .Build();

        await scheduler.ScheduleJob(job, trigger);
    }

    public async Task UnscheduleTopupJobAsync(string ruleId)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        var jobKey = new JobKey($"TopUpJob_{ruleId}", "TopUpGroup");
        
        await scheduler.DeleteJob(jobKey);
    }
}