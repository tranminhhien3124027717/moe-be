using MOE_System.Application.DTOs.Dashboard.Response;
using MOE_System.Application.Interfaces.Services;
using MOE_System.Domain.Entities;
using MOE_System.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using MOE_System.Domain.Enums;

namespace MOE_System.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<IReadOnlyList<ScheduledTopUpResponse>> GetTopUpTypesAsync(RuleTargetType type, CancellationToken cancellationToken)
    {
        return type switch 
        { 
            RuleTargetType.Batch => QueryScheduledTopUpAsync(RuleTargetType.Batch, cancellationToken),
            RuleTargetType.Individual => QueryScheduledTopUpAsync(RuleTargetType.Individual, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(type), $"The scheduled top-up type '{type}' is not recognized.")
        };
    }

    private async Task<IReadOnlyList<ScheduledTopUpResponse>> QueryScheduledTopUpAsync(RuleTargetType targetType, CancellationToken cancellationToken)
    {
        var batchRuleRepository = _unitOfWork.GetRepository<BatchRuleExecution>();
        var now = DateTime.UtcNow;

        var results = await batchRuleRepository.ToListAsync(
            predicate: br =>
                br.TopupRule != null &&
                br.BatchExecution != null &&
                br.TopupRule.RuleTargetType == targetType &&
                br.BatchExecution.ScheduledTime >= now &&
                br.BatchExecution.Status == TopUpStatus.Scheduled,
            include: query =>
            {
                query = query
                    .Include(x => x.TopupRule)
                    .Include(x => x.BatchExecution);

                if (targetType == RuleTargetType.Individual)
                {
                    query = query.Include(x => x.TopupRule!.Targets);
                }

                return query;
            },
            orderBy: query => query.OrderBy(x => x.BatchExecution!.ScheduledTime),
            take: 5,
            cancellationToken: cancellationToken
        );

        return results
            .Select(x =>
            {
                var rule = x.TopupRule!;
                var batch = x.BatchExecution!;

                var name = targetType == RuleTargetType.Batch
                    ? rule.RuleName
                    : rule.Targets!.Count.ToString();

                return new ScheduledTopUpResponse(
                    rule.Id,
                    name,
                    rule.TopupAmount,
                    batch.ScheduledTime,
                    batch.Status.ToString(),
                    targetType == RuleTargetType.Batch
                        ? rule.NumberOfAccountsAffected
                        : null
                );
            })
            .ToList();
    }

    public async Task<IReadOnlyList<RecentActivityResponse>> GetRecentActivitiesAsync(CancellationToken cancellationToken)
    {
        var educationAccountRepository = _unitOfWork.GetRepository<EducationAccount>();

        var results = await educationAccountRepository.ToListAsync(
            include: query => query
                .Include(e => e.AccountHolder),
            orderBy: query => query.OrderByDescending(e => e.CreatedAt),
            take: 10,
            cancellationToken: cancellationToken
        );

        return results.Select(e => new RecentActivityResponse(
            e.Id,
            e.AccountHolder!.FullName,
            e.AccountHolder.Email,
            e.CreatedAt
        )).ToList();
    }
}
