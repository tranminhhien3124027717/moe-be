using MOE_System.Application.Interfaces.Services;
using Quartz;

namespace MOE_System.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed class TopUpExecutionJob : IJob
{
    private readonly ITopUpService _topUpService;

    public TopUpExecutionJob(ITopUpService topUpService)
    {
        _topUpService = topUpService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var ruleId = context.MergedJobDataMap.GetString("RuleId");
        if (!string.IsNullOrEmpty(ruleId))
        {
            await _topUpService.ProcessTopUpExecutionAsync(ruleId, context.CancellationToken);
        }
    }
}