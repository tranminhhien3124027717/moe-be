using System.Linq.Expressions;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using MOE_System.Application.Common.Interfaces;
using MOE_System.Application.Common.Jobs;
using MOE_System.Application.DTOs.TopUp.Request;
using MOE_System.Application.DTOs.TopUp.Response;
using MOE_System.Application.Interfaces.Services;
using MOE_System.Domain.Entities;
using MOE_System.Domain.Enums;
using static MOE_System.Domain.Common.BaseException;

namespace MOE_System.Application.Services;

public class TopUpService : ITopUpService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITopUpScheduler _topUpScheduler;

    public TopUpService(IUnitOfWork unitOfWork, ITopUpScheduler topUpScheduler)
    {
        _unitOfWork = unitOfWork;
        _topUpScheduler = topUpScheduler;
    }

    public async Task CreateScheduledTopUpAsync(CreateScheduledTopUpRequest request, CancellationToken cancellationToken)
    {
        var scheduledTime = request.ExecuteImmediately
            ? DateTime.UtcNow
            : request.ScheduledTime!.Value;

        if (request.RuleTargetType == RuleTargetType.Individual &&
            request.TargetEducationAccountId != null &&
            request.TargetEducationAccountId.Any())
        {
            var requestedIds = request.TargetEducationAccountId.Distinct().ToList();
            var validAccounts = await _unitOfWork.GetRepository<EducationAccount>().ToListAsync(
                predicate: ea => requestedIds.Contains(ea.Id) && ea.IsActive,
                cancellationToken: cancellationToken
            );

            if (validAccounts.Count != requestedIds.Count)
            {
                var invalidIds = requestedIds.Except(validAccounts.Select(ea => ea.Id)).ToList();
                throw new BadRequestException($"The following education accounts are invalid or inactive: {string.Join(", ", invalidIds)}");
            }
        }

        var educationLevels = request.EducationLevelIds is { Count: > 0 }
            ? await _unitOfWork.GetRepository<EducationLevelDefinition>()
                .ToListAsync(
                    predicate: el => request.EducationLevelIds.Contains(el.Id),
                    cancellationToken: cancellationToken,
                    asTracking: true
                )
            : new List<EducationLevelDefinition>();

        var schoolingStatuses = request.SchoolingStatusIds is { Count: > 0 }
            ? await _unitOfWork.GetRepository<SchoolingStatusDefinition>()
                .ToListAsync(
                    predicate: ss => request.SchoolingStatusIds.Contains(ss.Id),
                    cancellationToken: cancellationToken,
                    asTracking: true
                )
            : new List<SchoolingStatusDefinition>();

        var batchExecution = new BatchExecution
        {
            ScheduledTime = scheduledTime,
            Status = TopUpStatus.Scheduled,
        };

        var topUpRule = new TopupRule
        {
            RuleName = request.RuleName,
            TopupAmount = request.TopupAmount,
            RuleTargetType = request.RuleTargetType,
            BatchFilterType = request.RuleTargetType == RuleTargetType.Batch
                ? request.BatchFilterType
                : null,

            MinAge = request.MinAge,
            MaxAge = request.MaxAge,
            MinBalance = request.MinBalance,
            MaxBalance = request.MaxBalance,
            ResidentialStatus = ResidentialStatus.SingaporeCitizen,

            ScheduledTime = scheduledTime,
            IsExecuted = false,
            Description = request.Description,
            InternalRemarks = request.InternalRemarks,
        };

        if (request.RuleTargetType == RuleTargetType.Individual &&
            request.TargetEducationAccountId != null &&
            request.TargetEducationAccountId.Any())
        {
            foreach (var eduAccId in request.TargetEducationAccountId)
            {
                topUpRule.Targets.Add(new TopupRuleTarget
                {
                    EducationAccountId = eduAccId
                });
            }
            topUpRule.NumberOfAccountsAffected = request.TargetEducationAccountId.Count;
        }

        foreach (var eduLevel in educationLevels)
        {
            topUpRule.AddEducationLevel(eduLevel);
        }

        foreach (var schoolingStatus in schoolingStatuses)
        {
            topUpRule.AddSchoolingStatus(schoolingStatus);
        }

        await _unitOfWork.GetRepository<BatchExecution>().InsertAsync(batchExecution);

        await _unitOfWork.GetRepository<TopupRule>().InsertAsync(topUpRule);

        await _unitOfWork.GetRepository<BatchRuleExecution>().InsertAsync(new BatchRuleExecution
        {
            BatchExecution = batchExecution,
            TopupRule = topUpRule
        });

        await _unitOfWork.SaveAsync();

        if (request.ExecuteImmediately)
        {
            await ProcessTopUpExecutionAsync(topUpRule.Id, cancellationToken);
            return;
        }

        await _topUpScheduler.ScheduleTopupJobAsync(topUpRule);
    }

    public async Task ProcessTopUpExecutionAsync(string ruleId, CancellationToken cancellationToken)
    {
        using var tx = await _unitOfWork.BeginTransactionAsync();

        try
        {
            var topupRuleRepository = _unitOfWork.GetRepository<TopupRule>();

            var topupRule = await topupRuleRepository.FirstOrDefaultAsync(
                predicate: tr => tr.Id == ruleId,
                include: query => query
                    .Include(tr => tr.EducationLevels)
                    .Include(tr => tr.SchoolingStatuses)
                    .Include(tr => tr.Targets)
                    .Include(tr => tr.BatchRuleExecutions)
                        .ThenInclude(bre => bre.BatchExecution),
                asTracking: true,
                cancellationToken: cancellationToken
            );

            if (topupRule == null || topupRule.IsExecuted)
            {
                return;
            }

            var batchExecution = topupRule.BatchRuleExecutions.FirstOrDefault()?.BatchExecution;

            if (batchExecution == null)
            {
                return;
            }

            if (batchExecution.Status == TopUpStatus.Cancelled || batchExecution.Status == TopUpStatus.Completed)
            {
                return;
            }

            List<EducationAccount> targetAccounts;
            if (topupRule.RuleTargetType == RuleTargetType.Individual)
            {
                // Only get active targets
                var activeTargets = topupRule.Targets.Where(t => t.IsActive).ToList();

                if (!activeTargets.Any())
                {
                    batchExecution.Status = TopUpStatus.Cancelled;
                    await _unitOfWork.SaveAsync();
                    _unitOfWork.CommitTransaction();
                    return;
                }

                var targetIds = activeTargets.Select(t => t.EducationAccountId).Distinct().ToList();

                targetAccounts = await _unitOfWork.GetRepository<EducationAccount>()
                    .ToListAsync(
                        predicate: ea => targetIds.Contains(ea.Id) && ea.IsActive,
                        asTracking: true,
                        cancellationToken: cancellationToken
                    );
            }
            else
            {
                var predicate = BuildBatchPredicate(topupRule);

                targetAccounts = await _unitOfWork.GetRepository<EducationAccount>()
                    .ToListAsync(
                        predicate: predicate.Expand(),
                        asTracking: true,
                        cancellationToken: cancellationToken
                    );

                var targetRepo = _unitOfWork.GetRepository<TopupRuleTarget>();

                foreach (var account in targetAccounts)
                {
                    targetRepo.Insert(new TopupRuleTarget
                    {
                        TopupRuleId = topupRule.Id,
                        EducationAccountId = account.Id
                    });
                }
            }

            var hocRepo = _unitOfWork.GetRepository<HistoryOfChange>();
            var snapshotRepo = _unitOfWork.GetRepository<TopupExecutionSnapshot>();

            foreach (var account in targetAccounts)
            {
                var before = account.Balance;
                var after = before + topupRule.TopupAmount;

                account.Balance = after;

                await hocRepo.InsertAsync(new HistoryOfChange
                {
                    EducationAccountId = account.Id,
                    ReferenceId = topupRule.Id,
                    Amount = topupRule.TopupAmount,
                    Type = ChangeType.TopUp,
                    BalanceBefore = before,
                    BalanceAfter = after,
                    Description = topupRule.Description ?? $"Top-up via rule '{topupRule.RuleName}'"
                });

                await snapshotRepo.InsertAsync(new TopupExecutionSnapshot
                {
                    TopupRuleId = topupRule.Id,
                    EducationAccountId = account.Id,
                    Amount = topupRule.TopupAmount,
                    BalanceBefore = before,
                    BalanceAfter = after,
                    ExecutedAt = DateTime.UtcNow
                });
            }

            topupRule.NumberOfAccountsAffected = targetAccounts.Count;
            topupRule.IsExecuted = true;

            batchExecution.ExecutedTime = DateTime.UtcNow;
            batchExecution.Status = TopUpStatus.Completed;

            await _unitOfWork.SaveAsync();
            _unitOfWork.CommitTransaction();
        }
        catch
        {
            _unitOfWork.RollBack();
            throw;
        }
    }

    private Expression<Func<EducationAccount, bool>> BuildBatchPredicate(TopupRule rule)
    {
        var predicate = PredicateBuilder.New<EducationAccount>(true);
        var currentYear = DateTime.UtcNow.Year;

        predicate = predicate.And(ea => ea.IsActive);

        predicate = predicate.And(ea =>
            ea.AccountHolder != null &&
            ea.AccountHolder.ResidentialStatus == ResidentialStatus.SingaporeCitizen.ToString());

        if (rule.MinAge.HasValue)
        {
            var maxBirthYear = currentYear - rule.MinAge.Value;
            predicate = predicate.And(ea =>
                ea.AccountHolder!.DateOfBirth.Year <= maxBirthYear);
        }

        if (rule.MaxAge.HasValue)
        {
            var minBirthYear = currentYear - rule.MaxAge.Value - 1;
            predicate = predicate.And(ea =>
                ea.AccountHolder!.DateOfBirth.Year >= minBirthYear);
        }

        if (rule.MinBalance.HasValue)
        {
            predicate = predicate.And(ea =>
                ea.Balance >= rule.MinBalance.Value);
        }

        if (rule.MaxBalance.HasValue)
        {
            predicate = predicate.And(ea =>
                ea.Balance <= rule.MaxBalance.Value);
        }

        if (rule.EducationLevels != null && rule.EducationLevels.Any())
        {
            var educationLevelNames = rule.EducationLevels
                .Select(rel => rel.Name)
                .ToList();

            predicate = predicate.And(ea =>
                ea.AccountHolder != null &&
                educationLevelNames.Contains(ea.AccountHolder.EducationLevel.ToString()));
        }

        if (rule.SchoolingStatuses.Count > 0)
        {
            var schoolingStatusNames = rule.SchoolingStatuses
                .Select(x => x.Name)
                .ToList();

            predicate = predicate.And(ea =>
                ea.AccountHolder != null &&
                schoolingStatusNames.Contains(ea.AccountHolder.SchoolingStatus.ToString()));
        }

        return predicate;
    }

    public async Task<CancelScheduledTopUpResponse> CancelScheduledTopUpAsync(
        string ruleId,
        CancelScheduledTopUpRequest request,
        CancellationToken cancellationToken)
    {
        return request.Type switch
        {
            RuleTargetType.Batch => await CancelBatchTopUpAsync(ruleId, cancellationToken),
            RuleTargetType.Individual => await CancelIndividualTargetAsync(ruleId, request.EducationAccountId!, cancellationToken),
            _ => throw new BadRequestException($"Unsupported rule target type: {request.Type}")
        };
    }

    private async Task<CancelScheduledTopUpResponse> CancelBatchTopUpAsync(
        string ruleId,
        CancellationToken cancellationToken)
    {
        using var tx = await _unitOfWork.BeginTransactionAsync();

        try
        {
            var topupRuleRepository = _unitOfWork.GetRepository<TopupRule>();

            var topupRule = await topupRuleRepository.FirstOrDefaultAsync(
                predicate: tr => tr.Id == ruleId,
                include: tr => tr.Include(x => x.BatchRuleExecutions)
                    .ThenInclude(bre => bre.BatchExecution),
                asTracking: true,
                cancellationToken: cancellationToken
            );

            if (topupRule == null)
            {
                throw new NotFoundException($"Top-up rule with ID '{ruleId}' not found");
            }

            if (topupRule.RuleTargetType != RuleTargetType.Batch)
            {
                throw new BadRequestException(
                    $"Type mismatch: Rule '{ruleId}' is {topupRule.RuleTargetType}, not Batch");
            }

            if (topupRule.IsExecuted)
            {
                throw new BadRequestException(
                    $"Cannot cancel top-up rule '{topupRule.RuleName}' - already executed at {topupRule.BatchRuleExecutions.FirstOrDefault()?.BatchExecution?.ExecutedTime:yyyy-MM-dd HH:mm:ss UTC}");
            }

            var batchExecution = topupRule.BatchRuleExecutions.FirstOrDefault()?.BatchExecution;

            if (batchExecution == null)
            {
                throw new InvalidOperationException($"Top-up rule '{ruleId}' has no associated batch execution");
            }

            if (batchExecution.Status == TopUpStatus.Cancelled)
            {
                return new CancelScheduledTopUpResponse
                {
                    RuleId = ruleId,
                    RuleName = topupRule.RuleName,
                    Type = RuleTargetType.Batch,
                    EntireRuleCancelled = true,
                    RemainingTargets = 0,
                    Message = "Batch rule was already cancelled"
                };
            }

            if (batchExecution.Status == TopUpStatus.Completed)
            {
                throw new BadRequestException(
                    $"Cannot cancel top-up rule '{topupRule.RuleName}' - already completed at {batchExecution.ExecutedTime:yyyy-MM-dd HH:mm:ss UTC}");
            }

            try
            {
                await _topUpScheduler.UnscheduleTopupJobAsync(topupRule.Id);
            }
            catch
            {
            }

            batchExecution.Status = TopUpStatus.Cancelled;

            await _unitOfWork.SaveAsync();
            _unitOfWork.CommitTransaction();

            return new CancelScheduledTopUpResponse
            {
                RuleId = ruleId,
                RuleName = topupRule.RuleName,
                Type = RuleTargetType.Batch,
                EntireRuleCancelled = true,
                RemainingTargets = 0,
                Message = "Batch rule cancelled successfully"
            };
        }
        catch
        {
            _unitOfWork.RollBack();
            throw;
        }
    }

    private async Task<CancelScheduledTopUpResponse> CancelIndividualTargetAsync(
        string ruleId,
        string educationAccountId,
        CancellationToken cancellationToken)
    {
        using var tx = await _unitOfWork.BeginTransactionAsync();

        try
        {
            var topupRuleRepository = _unitOfWork.GetRepository<TopupRule>();

            var topupRule = await topupRuleRepository.FirstOrDefaultAsync(
                predicate: tr => tr.Id == ruleId,
                include: tr => tr
                    .Include(x => x.BatchRuleExecutions)
                        .ThenInclude(bre => bre.BatchExecution)
                    .Include(x => x.Targets),
                asTracking: true,
                cancellationToken: cancellationToken
            );

            if (topupRule == null)
            {
                throw new NotFoundException($"Top-up rule with ID '{ruleId}' not found");
            }

            if (topupRule.RuleTargetType != RuleTargetType.Individual)
            {
                throw new BadRequestException(
                    $"Type mismatch: Rule '{ruleId}' is {topupRule.RuleTargetType}, not Individual");
            }

            if (topupRule.IsExecuted)
            {
                throw new BadRequestException(
                    $"Cannot cancel target - rule '{topupRule.RuleName}' has already been executed");
            }

            var batchExecution = topupRule.BatchRuleExecutions.FirstOrDefault()?.BatchExecution;

            if (batchExecution?.Status == TopUpStatus.Completed)
            {
                throw new BadRequestException("Cannot cancel target - rule has already been completed");
            }

            var target = topupRule.Targets.FirstOrDefault(t => t.EducationAccountId == educationAccountId);

            if (target == null)
            {
                throw new NotFoundException(
                    $"Target with EducationAccountId '{educationAccountId}' not found in rule '{ruleId}'");
            }

            if (!target.IsActive)
            {
                var activeTargets = topupRule.Targets.Count(t => t.IsActive);
                return new CancelScheduledTopUpResponse
                {
                    RuleId = ruleId,
                    RuleName = topupRule.RuleName,
                    Type = RuleTargetType.Individual,
                    EntireRuleCancelled = activeTargets == 0,
                    RemainingTargets = activeTargets,
                    Message = "Target was already cancelled"
                };
            }

            target.IsActive = false;

            if (topupRule.NumberOfAccountsAffected.HasValue && topupRule.NumberOfAccountsAffected > 0)
            {
                topupRule.NumberOfAccountsAffected--;
            }

            var remainingTargets = topupRule.Targets.Count(t => t.IsActive);
            var allTargetsCancelled = remainingTargets == 0;

            if (allTargetsCancelled && batchExecution != null)
            {
                batchExecution.Status = TopUpStatus.Cancelled;

                try
                {
                    await _topUpScheduler.UnscheduleTopupJobAsync(topupRule.Id);
                }
                catch
                {
                }
            }

            await _unitOfWork.SaveAsync();
            _unitOfWork.CommitTransaction();

            return new CancelScheduledTopUpResponse
            {
                RuleId = ruleId,
                RuleName = topupRule.RuleName,
                Type = RuleTargetType.Individual,
                EntireRuleCancelled = allTargetsCancelled,
                RemainingTargets = remainingTargets,
                Message = allTargetsCancelled
                    ? "Last target cancelled - entire rule cancelled"
                    : $"Target cancelled successfully. {remainingTargets} target(s) remaining"
            };
        }
        catch
        {
            _unitOfWork.RollBack();
            throw;
        }
    }



    public async Task<PaginatedTopUpScheduleResponse> GetTopUpSchedulesAsync(
        GetTopUpSchedulesRequest request,
        CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.GetRepository<TopupRule>();

        IQueryable<TopupRule> query = repo.Entities;

        // Apply filters first (before includes to optimize query)
        if (request.Types is { Count: > 0 })
        {
            query = query.Where(tr => request.Types.Contains(tr.RuleTargetType));
        }

        if (request.Statuses is { Count: > 0 })
        {
            var hasScheduled = request.Statuses.Contains(TopUpStatus.Scheduled);

            query = query.Where(tr =>
                tr.BatchRuleExecutions.Any(bre =>
                    bre.BatchExecution != null &&
                    request.Statuses.Contains(bre.BatchExecution.Status)
                )
                || (hasScheduled && !tr.BatchRuleExecutions.Any())
            );
        }

        if (request.ScheduledDateFrom.HasValue)
        {
            query = query.Where(tr => tr.ScheduledTime >= request.ScheduledDateFrom.Value);
        }

        if (request.ScheduledDateTo.HasValue)
        {
            query = query.Where(tr => tr.ScheduledTime <= request.ScheduledDateTo.Value);
        }

        // Include related entities
        query = query
            .Include(tr => tr.BatchRuleExecutions)
                .ThenInclude(bre => bre.BatchExecution)
            .Include(tr => tr.Targets)
                .ThenInclude(t => t.EducationAccount)
                    .ThenInclude(ea => ea.AccountHolder);

        // Apply sorting (excluding status which needs in-memory sorting)
        var shouldSortByStatus = request.SortBy?.ToLower() == "status";
        if (!shouldSortByStatus)
        {
            // Apply sorting with CreatedAt descending as default
            query = ApplySorting(query, request.SortBy, request.SortDescending);
        }
        else
        {
            // Default sorting for status sorting (will be sorted in-memory)
            // Sort by created date descending to show newest first
            query = query.OrderByDescending(tr => tr.CreatedAt);
        }

        // Fetch all matching data
        var allRules = await query.ToListAsync(cancellationToken);

        // Apply search filter in-memory (after loading related data)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();

            allRules = allRules.Where(tr =>
                tr.RuleName.ToLower().Contains(search) ||
                tr.Targets.Any(t =>
                    t.EducationAccount?.AccountHolder != null &&
                    (
                        t.EducationAccount.AccountHolder.FullName.ToLower().Contains(search) ||
                        t.EducationAccount.AccountHolder.NRIC.ToLower().Contains(search)
                    )
                )
            ).ToList();
        }

        // Apply status sorting in-memory if requested
        if (shouldSortByStatus)
        {
            allRules = request.SortDescending
                ? allRules.OrderByDescending(tr =>
                    tr.BatchRuleExecutions.FirstOrDefault()?.BatchExecution?.Status ?? TopUpStatus.Scheduled).ToList()
                : allRules.OrderBy(tr =>
                    tr.BatchRuleExecutions.FirstOrDefault()?.BatchExecution?.Status ?? TopUpStatus.Scheduled).ToList();
        }

        // Flatten the list - for Individual type with multiple targets, create one row per target
        var allItems = allRules.SelectMany(tr =>
        {
            var batchExecution = tr.BatchRuleExecutions.FirstOrDefault()?.BatchExecution;

            // For Individual type, expand to multiple rows (one per target)
            if (tr.RuleTargetType == RuleTargetType.Individual && tr.Targets.Any())
            {
                return tr.Targets
                    .Where(t => t.EducationAccount?.AccountHolder != null)
                    .Select(target => new TopUpScheduleResponse
                    {
                        Id = tr.Id,
                        RuleName = tr.RuleName,
                        Type = tr.RuleTargetType,
                        Amount = tr.TopupAmount,
                        Status = !target.IsActive
                            ? TopUpStatus.Cancelled
                            : (batchExecution?.Status ?? TopUpStatus.Scheduled),
                        ScheduledTime = tr.ScheduledTime,
                        ExecutedTime = batchExecution?.ExecutedTime,
                        CreatedDate = tr.CreatedAt,
                        CreatedBy = "Admin",
                        NumberOfAccountsAffected = tr.NumberOfAccountsAffected,

                        // Target-specific fields (flat)
                        TargetEducationAccountId = target.EducationAccountId,
                        TargetAccountHolderName = target.EducationAccount!.AccountHolder!.FullName,
                        TargetAccountHolderNric = target.EducationAccount.AccountHolder.NRIC
                    });
            }

            // For Batch type, single row
            return new[]
            {
                new TopUpScheduleResponse
                {
                    Id = tr.Id,
                    RuleName = tr.RuleName,
                    Type = tr.RuleTargetType,
                    Amount = tr.TopupAmount,
                    Status = batchExecution?.Status ?? TopUpStatus.Scheduled,
                    ScheduledTime = tr.ScheduledTime,
                    ExecutedTime = batchExecution?.ExecutedTime,
                    CreatedDate = tr.CreatedAt,
                    CreatedBy = "Admin",
                    NumberOfAccountsAffected = tr.NumberOfAccountsAffected,
                    
                    // No target info for Batch
                    TargetEducationAccountId = null,
                    TargetAccountHolderName = null,
                    TargetAccountHolderNric = null
                }
            };
        }).ToList();

        var totalCount = allItems.Count;

        var items = allItems
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

        return new PaginatedTopUpScheduleResponse
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    public async Task<List<FilteredAccountHolderResponse>> GetSingaporeCitizenAccountHoldersAsync(string? search, CancellationToken cancellationToken)
    {
        var accountHolderRepository = _unitOfWork.GetRepository<AccountHolder>();
        var educationAccountRepository = _unitOfWork.GetRepository<EducationAccount>();

        var citizenStatus = ResidentialStatus.SingaporeCitizen.ToString();

        var accountHolders = await accountHolderRepository.ToListAsync(
            predicate: ah => ah.ResidentialStatus == citizenStatus && ah.EducationAccount!.IsActive,
            include: query => query.Include(ah => ah.EducationAccount),
            cancellationToken: cancellationToken
        );

        var response = accountHolders
            .Where(ah => ah.EducationAccount != null)
            .Select(ah => new FilteredAccountHolderResponse
            {
                Id = ah.Id,
                EducationAccountId = ah.EducationAccount!.Id,
                FullName = ah.FullName,
                NRIC = ah.NRIC,
                Age = CalculateAge(ah.DateOfBirth),
                Balance = ah.EducationAccount!.Balance,
                EducationLevel = ah.EducationLevel.ToFriendlyString(),
                SchoolingStatus = ah.SchoolingStatus.ToFriendlyString()
            });

        // Apply search filter (applied in-memory after fetching from DB)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            response = response.Where(ah =>
                ah.FullName.ToLower().Contains(searchLower) ||
                ah.NRIC.ToLower().Contains(searchLower));
        }

        return response.ToList();
    }

    public async Task<List<FilteredAccountHolderResponse>> GetFilteredAccountHoldersAsync(GetFilteredAccountHoldersRequest request, CancellationToken cancellationToken)
    {
        var accountHolderRepository = _unitOfWork.GetRepository<AccountHolder>();
        var citizenStatus = ResidentialStatus.SingaporeCitizen.ToString();

        // Build predicate
        var predicate = PredicateBuilder.New<AccountHolder>(true);
        predicate = predicate.And(ah => ah.ResidentialStatus == citizenStatus);
        predicate = predicate.And(ah => ah.EducationAccount != null && ah.EducationAccount!.IsActive);

        var today = DateTime.UtcNow;

        // Age range filter
        if (request.MinAge.HasValue)
        {
            var latestBirthYear = today.Year - request.MinAge.Value;
            predicate = predicate.And(ah => ah.DateOfBirth.Year <= latestBirthYear);
        }

        if (request.MaxAge.HasValue)
        {
            var earliestBirthYear = today.Year - request.MaxAge.Value - 1;
            predicate = predicate.And(ah => ah.DateOfBirth.Year >= earliestBirthYear);
        }

        // Balance range filter
        if (request.MinBalance.HasValue)
        {
            predicate = predicate.And(ah => ah.EducationAccount!.Balance >= request.MinBalance.Value);
        }

        if (request.MaxBalance.HasValue)
        {
            predicate = predicate.And(ah => ah.EducationAccount!.Balance <= request.MaxBalance.Value);
        }

        List<EducationLevel>? educationLevels = null;
        if (request.EducationLevelsIds is { Count: > 0 })
        {
            var defs = await _unitOfWork
                .GetRepository<EducationLevelDefinition>()
                .ToListAsync(
                    predicate: el => request.EducationLevelsIds.Contains(el.Id),
                    cancellationToken: cancellationToken
                );

            educationLevels = defs
                .Select(d => Enum.Parse<EducationLevel>(d.Name))
                .ToList();

            predicate = predicate.And(ah => educationLevels.Contains(ah.EducationLevel));
        }

        List<SchoolingStatus>? schoolingStatuses = null;
        if (request.SchoolingStatusesIds is { Count: > 0 })
        {
            var defs = await _unitOfWork
                .GetRepository<SchoolingStatusDefinition>()
                .ToListAsync(
                    predicate: ss => request.SchoolingStatusesIds.Contains(ss.Id),
                    cancellationToken: cancellationToken
                );

            schoolingStatuses = defs
                .Select(d => Enum.Parse<SchoolingStatus>(d.Name))
                .ToList();

            predicate = predicate.And(ah => schoolingStatuses.Contains(ah.SchoolingStatus));
        }

        var accountHolders = await accountHolderRepository.ToListAsync(
            predicate: predicate,
            include: query => query.Include(ah => ah.EducationAccount),
            cancellationToken: cancellationToken
        );

        var response = accountHolders.Select(ah => new FilteredAccountHolderResponse
        {
            Id = ah.Id,
            EducationAccountId = ah.EducationAccount!.Id,
            FullName = ah.FullName,
            NRIC = ah.NRIC,
            Age = CalculateAge(ah.DateOfBirth),
            Balance = ah.EducationAccount!.Balance,
            EducationLevel = ah.EducationLevel.ToFriendlyString(),
            SchoolingStatus = ah.SchoolingStatus.ToFriendlyString()
        }).ToList();

        return response;
    }

    private IQueryable<TopupRule> ApplySorting(
        IQueryable<TopupRule> query,
        string? sortBy,
        bool sortDescending)
    {
        // Note: Status sorting is handled in-memory after data fetch
        // because it requires navigating through BatchRuleExecutions
        return sortBy?.ToLower() switch
        {
            "type" => sortDescending
                ? query.OrderByDescending(tr => tr.RuleTargetType)
                : query.OrderBy(tr => tr.RuleTargetType),

            "name" => sortDescending
                ? query.OrderByDescending(tr => tr.RuleName)
                : query.OrderBy(tr => tr.RuleName),

            "amount" => sortDescending
                ? query.OrderByDescending(tr => tr.TopupAmount)
                : query.OrderBy(tr => tr.TopupAmount),

            "scheduleddate" => sortDescending
                ? query.OrderByDescending(tr => tr.ScheduledTime)
                : query.OrderBy(tr => tr.ScheduledTime),

            "createddate" => sortDescending
                ? query.OrderByDescending(tr => tr.CreatedAt)
                : query.OrderBy(tr => tr.CreatedAt),

            // Default: sort by created date descending (most recent first)
            _ => query.OrderByDescending(tr => tr.CreatedAt)
        };
    }

    private int CalculateAge(DateTime dateOfBirth)
    {
        var today = DateTime.UtcNow;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age)) age--;
        return age;
    }

    public async Task<TopupRuleDetailResponse> GetTopupRuleDetailAsync(
        string ruleId,
        string? educationAccountId,
        CancellationToken cancellationToken)
    {
        var topupRuleRepository = _unitOfWork.GetRepository<TopupRule>();

        var topupRule = await topupRuleRepository.FirstOrDefaultAsync(
            predicate: tr => tr.Id == ruleId,
            include: query => query
                .Include(tr => tr.BatchRuleExecutions)
                    .ThenInclude(bre => bre.BatchExecution)
                .Include(tr => tr.Targets)
                    .ThenInclude(t => t.EducationAccount)
                        .ThenInclude(ea => ea.AccountHolder)
                .Include(tr => tr.EducationLevels)
                .Include(tr => tr.SchoolingStatuses),
            cancellationToken: cancellationToken
        );

        if (topupRule == null)
        {
            throw new NotFoundException("Top-up rule not found.");
        }

        var batchExecution = topupRule.BatchRuleExecutions.FirstOrDefault()?.BatchExecution;
        var status = batchExecution?.Status ?? TopUpStatus.Scheduled;

        if (topupRule.RuleTargetType == RuleTargetType.Batch)
        {
            return await BuildBatchDetailResponse(topupRule, status, batchExecution?.ExecutedTime, cancellationToken);
        }
        else
        {
            return BuildIndividualDetailResponse(topupRule, status, batchExecution?.ExecutedTime, educationAccountId);
        }
    }

    private async Task<TopupRuleDetailResponse> BuildBatchDetailResponse(
        TopupRule topupRule,
        TopUpStatus status,
        DateTime? executedTime,
        CancellationToken cancellationToken)
    {
        int eligibleAccounts;

        if (topupRule.IsExecuted)
        {
            eligibleAccounts = topupRule.NumberOfAccountsAffected ?? 0;
        }
        else
        {
            var predicate = BuildBatchPredicate(topupRule);
            var accounts = await _unitOfWork.GetRepository<EducationAccount>().ToListAsync(
                predicate: predicate.Expand(),
                cancellationToken: cancellationToken
            );
            eligibleAccounts = accounts.Count;
        }

        var totalDisbursement = eligibleAccounts * topupRule.TopupAmount;

        var educationLevels = topupRule.EducationLevels.Any()
            ? topupRule.EducationLevels.Select(el => el.Name).ToList()
            : null;

        var schoolingStatuses = topupRule.SchoolingStatuses.Any()
            ? topupRule.SchoolingStatuses.Select(ss => ss.Name).ToList()
            : null;

        return new TopupRuleDetailResponse
        {
            Id = topupRule.Id,
            Type = RuleTargetType.Batch,
            RuleName = topupRule.RuleName,
            Description = topupRule.Description,
            InternalRemarks = topupRule.InternalRemarks,
            AmountPerAccount = topupRule.TopupAmount,
            Status = status,
            ScheduledDate = topupRule.ScheduledTime,
            ExecutedTime = executedTime,
            EligibleAccounts = eligibleAccounts,
            TotalDisbursement = totalDisbursement,
            TopupRules = new TopupRuleCriteriaResponse
            {
                TargetingType = topupRule.BatchFilterType ?? BatchFilterType.Everyone,
                MinAge = topupRule.MinAge,
                MaxAge = topupRule.MaxAge,
                MinBalance = topupRule.MinBalance,
                MaxBalance = topupRule.MaxBalance,
                EducationLevels = educationLevels,
                SchoolingStatuses = schoolingStatuses
            }
        };
    }

    private TopupRuleDetailResponse BuildIndividualDetailResponse(
        TopupRule topupRule,
        TopUpStatus status,
        DateTime? executedTime,
        string? educationAccountId)
    {
        if (!string.IsNullOrWhiteSpace(educationAccountId))
        {
            var target = topupRule.Targets.FirstOrDefault(t => t.EducationAccountId == educationAccountId);

            if (target == null)
            {
                throw new NotFoundException(
                    $"Target with EducationAccountId '{educationAccountId}' not found in rule '{topupRule.Id}'");
            }

            var accountHolder = target.EducationAccount?.AccountHolder;

            if (accountHolder == null)
            {
                throw new NotFoundException("Account holder information not found.");
            }

            return new TopupRuleDetailResponse
            {
                Id = topupRule.Id,
                Type = RuleTargetType.Individual,
                AccountName = accountHolder.FullName,
                AccountId = educationAccountId,
                Description = topupRule.Description,
                InternalRemarks = topupRule.InternalRemarks,
                AmountPerAccount = topupRule.TopupAmount,
                Status = !target.IsActive ? TopUpStatus.Cancelled : status,
                ScheduledDate = topupRule.ScheduledTime,
                ExecutedTime = executedTime
            };
        }

        var firstTarget = topupRule.Targets.FirstOrDefault();
        var firstAccountHolder = firstTarget?.EducationAccount?.AccountHolder;

        return new TopupRuleDetailResponse
        {
            Id = topupRule.Id,
            Type = RuleTargetType.Individual,
            AccountName = firstAccountHolder?.FullName,
            AccountId = firstTarget?.EducationAccountId,
            Description = topupRule.Description,
            InternalRemarks = topupRule.InternalRemarks,
            AmountPerAccount = topupRule.TopupAmount,
            Status = (firstTarget != null && !firstTarget.IsActive) ? TopUpStatus.Cancelled : status,
            ScheduledDate = topupRule.ScheduledTime,
            ExecutedTime = executedTime
        };
    }

    public async Task<BatchRuleAffectedAccountsResponse> GetBatchRuleAffectedAccountsAsync(
        string ruleId,
        GetBatchRuleAffectedAccountsRequest request,
        CancellationToken cancellationToken)
    {
        var topupRuleRepository = _unitOfWork.GetRepository<TopupRule>();

        var topupRule = await topupRuleRepository.FirstOrDefaultAsync(
            predicate: tr => tr.Id == ruleId,
            include: query => query
                .Include(tr => tr.EducationLevels)
                .Include(tr => tr.SchoolingStatuses),
            cancellationToken: cancellationToken
        );

        if (topupRule == null)
        {
            throw new NotFoundException("Top-up rule not found.");
        }

        if (topupRule.RuleTargetType != RuleTargetType.Batch)
        {
            throw new BadRequestException("This endpoint is only for batch rules.");
        }

        List<AffectedAccountDetailResponse> accountDetails;

        if (topupRule.IsExecuted)
        {
            var snapshotRepo = _unitOfWork.GetRepository<TopupExecutionSnapshot>();

            var query = snapshotRepo.Entities
                .Where(s => s.TopupRuleId == ruleId)
                .Include(s => s.EducationAccount)
                    .ThenInclude(ea => ea!.AccountHolder)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchLower = request.Search.ToLower();
                query = query.Where(s =>
                    s.EducationAccount!.AccountHolder!.FirstName.ToLower().Contains(searchLower) ||
                    s.EducationAccount.AccountHolder.LastName.ToLower().Contains(searchLower) ||
                    s.EducationAccount.AccountHolder.NRIC.ToLower().Contains(searchLower));
            }

            var records = await query.ToListAsync(cancellationToken);

            accountDetails = records.Select(s => new AffectedAccountDetailResponse
            {
                AccountHolderId = s.EducationAccount!.AccountHolderId,
                Name = s.EducationAccount.AccountHolder!.FullName,
                NRIC = s.EducationAccount.AccountHolder.NRIC,
                CurrentBalance = s.BalanceBefore,
                Age = CalculateAge(s.EducationAccount.AccountHolder.DateOfBirth),
                EducationLevel = s.EducationAccount.AccountHolder.EducationLevel,
                SchoolingStatus = s.EducationAccount.AccountHolder.SchoolingStatus,
                TopupAmount = s.Amount,
                NewBalance = s.BalanceAfter
            }).ToList();
        }
        else
        {
            var predicate = BuildBatchPredicate(topupRule);
            var eduAccountRepo = _unitOfWork.GetRepository<EducationAccount>();

            var query = eduAccountRepo.Entities
                .Include(ea => ea.AccountHolder)
                .Where(predicate.Expand())
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchLower = request.Search.ToLower();
                query = query.Where(ea =>
                    ea.AccountHolder!.FirstName.ToLower().Contains(searchLower) ||
                    ea.AccountHolder.LastName.ToLower().Contains(searchLower) ||
                    ea.AccountHolder.NRIC.ToLower().Contains(searchLower));
            }

            var accounts = await query.ToListAsync(cancellationToken);

            accountDetails = accounts.Select(ea => new AffectedAccountDetailResponse
            {
                AccountHolderId = ea.AccountHolderId,
                Name = ea.AccountHolder!.FullName,
                NRIC = ea.AccountHolder.NRIC,
                CurrentBalance = ea.Balance,
                Age = CalculateAge(ea.AccountHolder.DateOfBirth),
                EducationLevel = ea.AccountHolder.EducationLevel,
                SchoolingStatus = ea.AccountHolder.SchoolingStatus,
                TopupAmount = topupRule.TopupAmount,
                NewBalance = ea.Balance + topupRule.TopupAmount
            }).ToList();
        }

        var response = new BatchRuleAffectedAccountsResponse
        {
            TotalAccounts = accountDetails.Count,
            AmountPerAccount = topupRule.TopupAmount,
            TotalDisbursement = accountDetails.Count * topupRule.TopupAmount,
            Accounts = accountDetails
        };

        return response;
    }

    public async Task<TopupCustomizeFilterResponse> GetTopuCustomizeFilterAsync(CancellationToken cancellationToken)
    {
        var educationLevels = await _unitOfWork.GetRepository<EducationLevelDefinition>()
            .ToListAsync(cancellationToken: cancellationToken);

        var schoolingStatuses = await _unitOfWork.GetRepository<SchoolingStatusDefinition>()
            .ToListAsync(cancellationToken: cancellationToken);

        return new TopupCustomizeFilterResponse
        {
            EducationLevels = educationLevels.Select(el => new EducationLevelDefinitionResponse
            {
                Id = el.Id,
                Name = el.Name
            }).ToList(),
            SchoolingStatuses = schoolingStatuses.Select(ss => new SchoolingStatusDefinitionResponse
            {
                Id = ss.Id,
                Name = ss.Name
            }).ToList()
        };
    }
}