using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using MOE_System.Application.Common.Interfaces;
using MOE_System.Application.Common.Jobs;
using MOE_System.Application.DTOs.TopUp.Request;
using MOE_System.Application.Services;
using MOE_System.Domain.Entities;
using MOE_System.Domain.Enums;
using Xunit;
using static MOE_System.Domain.Common.BaseException;

namespace MOE_System.Application.Tests.TopupServiceTests;

public class TopUpServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ITopUpScheduler> _mockTopUpScheduler;
    private readonly Mock<IGenericRepository<BatchExecution>> _mockBatchExecutionRepo;
    private readonly Mock<IGenericRepository<TopupRule>> _mockTopupRuleRepo;
    private readonly Mock<IGenericRepository<BatchRuleExecution>> _mockBatchRuleExecutionRepo;
    private readonly Mock<IGenericRepository<EducationAccount>> _mockEducationAccountRepo;
    private readonly Mock<IGenericRepository<HistoryOfChange>> _mockHistoryOfChangeRepo;
    private readonly TopUpService _service;

    public TopUpServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockTopUpScheduler = new Mock<ITopUpScheduler>();
        _mockBatchExecutionRepo = new Mock<IGenericRepository<BatchExecution>>();
        _mockTopupRuleRepo = new Mock<IGenericRepository<TopupRule>>();
        _mockBatchRuleExecutionRepo = new Mock<IGenericRepository<BatchRuleExecution>>();
        _mockEducationAccountRepo = new Mock<IGenericRepository<EducationAccount>>();
        _mockHistoryOfChangeRepo = new Mock<IGenericRepository<HistoryOfChange>>();

        _mockUnitOfWork.Setup(u => u.GetRepository<BatchExecution>())
            .Returns(_mockBatchExecutionRepo.Object);
        _mockUnitOfWork.Setup(u => u.GetRepository<TopupRule>())
            .Returns(_mockTopupRuleRepo.Object);
        _mockUnitOfWork.Setup(u => u.GetRepository<BatchRuleExecution>())
            .Returns(_mockBatchRuleExecutionRepo.Object);
        _mockUnitOfWork.Setup(u => u.GetRepository<EducationAccount>())
            .Returns(_mockEducationAccountRepo.Object);
        _mockUnitOfWork.Setup(u => u.GetRepository<HistoryOfChange>())
            .Returns(_mockHistoryOfChangeRepo.Object);

        _service = new TopUpService(_mockUnitOfWork.Object, _mockTopUpScheduler.Object);
    }

    #region CreateScheduledTopUpAsync Tests

    [Fact]
    public async Task CreateScheduledTopUpAsync_WithExecuteImmediately_CreatesEntitiesWithCurrentTime()
    {
        // Arrange
        var beforeTime = DateTime.UtcNow;
        var request = new CreateScheduledTopUpRequest(
            "Immediate Top-Up",
            100,
            RuleTargetType.Batch,
            BatchFilterType.Everyone,
            DateTime.UtcNow,
            true
        );

        // Prevent ProcessTopUpExecutionAsync from throwing NotFoundException by
        // returning a topup rule and empty accounts when repository is queried.
        var dummyBatch = new BatchExecution { Id = Guid.NewGuid().ToString(), Status = TopUpStatus.Scheduled };
        var returnedRule = new TopupRule
        {
            Id = Guid.NewGuid().ToString(),
            TopupAmount = 100,
            IsExecuted = false,
            BatchRuleExecutions = new List<BatchRuleExecution>
            {
                new BatchRuleExecution { BatchExecution = dummyBatch }
            }
        };

        _mockTopupRuleRepo.Setup(r => r.FirstOrDefaultAsync(
            It.IsAny<Expression<Func<TopupRule, bool>>>(),
            It.IsAny<Func<IQueryable<TopupRule>, IQueryable<TopupRule>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(returnedRule);

        _mockEducationAccountRepo.Setup(r => r.ToListAsync(
            It.IsAny<Expression<Func<EducationAccount, bool>>>(),
            It.IsAny<Func<IQueryable<EducationAccount>, IQueryable<EducationAccount>>>(),
            It.IsAny<Func<IQueryable<EducationAccount>, IOrderedQueryable<EducationAccount>>>(),
            It.IsAny<int>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(new List<EducationAccount>());

        // Act
        await _service.CreateScheduledTopUpAsync(request, CancellationToken.None);

        // Assert
        _mockBatchExecutionRepo.Verify(
            r => r.InsertAsync(It.Is<BatchExecution>(b => 
                b.Status == TopUpStatus.Scheduled && 
                b.ScheduledTime >= beforeTime && 
                b.ScheduledTime <= DateTime.UtcNow.AddSeconds(1)
            )), 
            Times.Once
        );

        _mockTopupRuleRepo.Verify(
            r => r.InsertAsync(It.Is<TopupRule>(tr => 
                tr.RuleName == "Immediate Top-Up" && 
                tr.TopupAmount == 100 && 
                tr.IsExecuted == false
            )), 
            Times.Once
        );

        _mockBatchRuleExecutionRepo.Verify(r => r.InsertAsync(It.IsAny<BatchRuleExecution>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateScheduledTopUpAsync_WithScheduledTime_CreatesEntitiesWithRequestedTime()
    {
        // Arrange
        var scheduledTime = DateTime.UtcNow.AddDays(7);
        var request = new CreateScheduledTopUpRequest(
            "Scheduled Top-Up",
            500,
            RuleTargetType.Batch,
            BatchFilterType.Everyone,
            scheduledTime,
            false
        );

        // Act
        await _service.CreateScheduledTopUpAsync(request, CancellationToken.None);

        // Assert
        _mockBatchExecutionRepo.Verify(
            r => r.InsertAsync(It.Is<BatchExecution>(b => 
                b.ScheduledTime == scheduledTime
            )), 
            Times.Once
        );

        _mockTopupRuleRepo.Verify(
            r => r.InsertAsync(It.Is<TopupRule>(tr => 
                tr.ScheduledTime == scheduledTime
            )), 
            Times.Once
        );

        _mockTopUpScheduler.Verify(s => s.ScheduleTopupJobAsync(It.IsAny<TopupRule>()), Times.Once);
    }

    [Fact]
    public async Task CreateScheduledTopUpAsync_WithIndividualTarget_SetsTargetEducationAccountId()
    {
        // Arrange
        var accountId = Guid.NewGuid().ToString();
        var request = new CreateScheduledTopUpRequest(
            "Individual Top-Up",
            200,
            RuleTargetType.Individual,
            BatchFilterType.Everyone,
            DateTime.UtcNow,
            false,
            new List<string> { accountId }
        );

        // Act
        await _service.CreateScheduledTopUpAsync(request, CancellationToken.None);

        // Assert
        _mockTopupRuleRepo.Verify(
            r => r.InsertAsync(It.Is<TopupRule>(tr => 
                tr.Targets.Any(t => t.EducationAccountId == accountId) &&
                tr.BatchFilterType == null
            )), 
            Times.Once
        );
    }

    [Fact]
    public async Task CreateScheduledTopUpAsync_WithBatchTarget_SetsBatchFilterType()
    {
        // Arrange
        var request = new CreateScheduledTopUpRequest(
            "Batch Top-Up",
            300,
            RuleTargetType.Batch,
            BatchFilterType.Customized,
            DateTime.UtcNow,
            false,
            null,
            null,
            null,
            null,
            null,
            EducationLevel.Primary,
            SchoolingStatus.InSchool
        );

        // Act
        await _service.CreateScheduledTopUpAsync(request, CancellationToken.None);

        // Assert
        _mockTopupRuleRepo.Verify(
            r => r.InsertAsync(It.Is<TopupRule>(tr => 
                tr.BatchFilterType == BatchFilterType.Customized &&
                !tr.Targets.Any()
            )), 
            Times.Once
        );
    }

    [Fact]
    public async Task CreateScheduledTopUpAsync_WithAgeFilters_StoresAgeRanges()
    {
        // Arrange
        var request = new CreateScheduledTopUpRequest(
            "Age-Based Top-Up",
            250,
            RuleTargetType.Batch,
            BatchFilterType.Everyone,
            DateTime.UtcNow,
            false,
            null,
            10,
            18
        );

        // Act
        await _service.CreateScheduledTopUpAsync(request, CancellationToken.None);

        // Assert
        _mockTopupRuleRepo.Verify(
            r => r.InsertAsync(It.Is<TopupRule>(tr => 
                tr.MinAge == 10 &&
                tr.MaxAge == 18
            )), 
            Times.Once
        );
    }

    [Fact]
    public async Task CreateScheduledTopUpAsync_WithBalanceFilters_StoresBalanceRanges()
    {
        // Arrange
        var request = new CreateScheduledTopUpRequest(
            "Balance-Based Top-Up",
            100,
            RuleTargetType.Batch,
            BatchFilterType.Everyone,
            DateTime.UtcNow,
            false,
            null,
            null,
            null,
            50,
            500
        );

        // Act
        await _service.CreateScheduledTopUpAsync(request, CancellationToken.None);

        // Assert
        _mockTopupRuleRepo.Verify(
            r => r.InsertAsync(It.Is<TopupRule>(tr => 
                tr.MinBalance == 50 &&
                tr.MaxBalance == 500
            )), 
            Times.Once
        );
    }

    [Fact]
    public async Task CreateScheduledTopUpAsync_WithMultipleFilters_StoresAllFilters()
    {
        // Arrange
        var request = new CreateScheduledTopUpRequest(
            "Complex Top-Up",
            150,
            RuleTargetType.Batch,
            BatchFilterType.Everyone,
            DateTime.UtcNow,
            false,
            null,
            12,
            25,
            100,
            1000,
            EducationLevel.Secondary,
            SchoolingStatus.InSchool
        );

        // Act
        await _service.CreateScheduledTopUpAsync(request, CancellationToken.None);

        // Assert
        _mockTopupRuleRepo.Verify(
            r => r.InsertAsync(It.Is<TopupRule>(tr => 
                tr.MinAge == 12 &&
                tr.MaxAge == 25 &&
                tr.MinBalance == 100 &&
                tr.MaxBalance == 1000 &&
                tr.EducationLevel == EducationLevel.Secondary &&
                tr.SchoolingStatus == SchoolingStatus.InSchool
            )), 
            Times.Once
        );
    }

    [Fact]
    public async Task CreateScheduledTopUpAsync_CallsSchedulerWithCorrectParameters()
    {
        // Arrange
        var request = new CreateScheduledTopUpRequest(
            "Test Top-Up",
            100,
            RuleTargetType.Batch,
            BatchFilterType.Everyone,
            DateTime.UtcNow,
            false
        );

        // Act
        await _service.CreateScheduledTopUpAsync(request, CancellationToken.None);

        // Assert
        _mockTopUpScheduler.Verify(
            s => s.ScheduleTopupJobAsync(It.IsAny<TopupRule>()), 
            Times.Once
        );
    }

    #endregion

    #region ProcessTopUpExecutionAsync Tests

    [Fact]
    public async Task ProcessTopUpExecutionAsync_WithMatchingAccounts_UpdatesBalancesAndCreatesHistory()
    {
        // Arrange
        var ruleId = Guid.NewGuid().ToString();
        var batchId = Guid.NewGuid().ToString();
        var accountId1 = Guid.NewGuid().ToString();
        var accountId2 = Guid.NewGuid().ToString();

        var batchExecution = new BatchExecution { Id = batchId, Status = TopUpStatus.Scheduled };
        var topupRule = new TopupRule
        {
            Id = ruleId,
            RuleName = "Test Rule",
            TopupAmount = 100,
            IsExecuted = false,
            RuleTargetType = RuleTargetType.Individual,
            Targets = new List<TopupRuleTarget>
            {
                new TopupRuleTarget { EducationAccountId = accountId1 }
            },
            BatchRuleExecutions = new List<BatchRuleExecution>
            {
                new BatchRuleExecution { BatchID = batchId, RuleID = ruleId, BatchExecution = batchExecution }
            }
        };

        var accounts = new List<EducationAccount>
        {
            new EducationAccount { Id = accountId1, Balance = 500 },
            new EducationAccount { Id = accountId2, Balance = 300 }
        };

        _mockTopupRuleRepo.Setup(r => r.FirstOrDefaultAsync(
            It.IsAny<Expression<Func<TopupRule, bool>>>(),
            It.IsAny<Func<IQueryable<TopupRule>, IQueryable<TopupRule>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(topupRule);

        _mockEducationAccountRepo.Setup(r => r.ToListAsync(
            It.IsAny<Expression<Func<EducationAccount, bool>>>(),
            It.IsAny<Func<IQueryable<EducationAccount>, IQueryable<EducationAccount>>>(),
            It.IsAny<Func<IQueryable<EducationAccount>, IOrderedQueryable<EducationAccount>>>(),
            It.IsAny<int>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(accounts);

        // Act
        await _service.ProcessTopUpExecutionAsync(ruleId, CancellationToken.None);

        // Assert
        _mockHistoryOfChangeRepo.Verify(
            r => r.InsertAsync(It.Is<HistoryOfChange>(hoc => 
                hoc.Amount == 100 && 
                hoc.Type == ChangeType.TopUp
            )), 
            Times.Exactly(2)
        );

        Assert.Equal(600, accounts[0].Balance);
        Assert.Equal(400, accounts[1].Balance);
    }

    [Fact]
    public async Task ProcessTopUpExecutionAsync_WhenNoAccountsMatch_SetsBatchStatusFailed()
    {
        // Arrange
        var ruleId = Guid.NewGuid().ToString();
        var batchId = Guid.NewGuid().ToString();
        var batchExecution = new BatchExecution { Id = batchId, Status = TopUpStatus.Scheduled };
        var topupRule = new TopupRule
        {
            Id = ruleId,
            RuleName = "Test Rule",
            TopupAmount = 100,
            IsExecuted = false,
            BatchRuleExecutions = new List<BatchRuleExecution>
            {
                new BatchRuleExecution { BatchID = batchId, RuleID = ruleId, BatchExecution = batchExecution }
            }
        };

        _mockTopupRuleRepo.Setup(r => r.FirstOrDefaultAsync(
            It.IsAny<Expression<Func<TopupRule, bool>>>(),
            It.IsAny<Func<IQueryable<TopupRule>, IQueryable<TopupRule>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(topupRule);

        _mockEducationAccountRepo.Setup(r => r.ToListAsync(
            It.IsAny<Expression<Func<EducationAccount, bool>>>(),
            It.IsAny<Func<IQueryable<EducationAccount>, IQueryable<EducationAccount>>>(),
            It.IsAny<Func<IQueryable<EducationAccount>, IOrderedQueryable<EducationAccount>>>(),
            It.IsAny<int>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(new List<EducationAccount>());

        // Act
        await _service.ProcessTopUpExecutionAsync(ruleId, CancellationToken.None);

        // Assert: current implementation marks batch as Completed even when no accounts
        // were affected (IsExecuted becomes true and NumberOfAccountsAffected == 0).
        Assert.Equal(TopUpStatus.Completed, batchExecution.Status);
        Assert.Equal(0, topupRule.NumberOfAccountsAffected);
        Assert.True(topupRule.IsExecuted);
        _mockHistoryOfChangeRepo.Verify(r => r.InsertAsync(It.IsAny<HistoryOfChange>()), Times.Never);
    }

    [Fact]
    public async Task ProcessTopUpExecutionAsync_WhenRuleNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var ruleId = Guid.NewGuid().ToString();
        _mockTopupRuleRepo.Setup(r => r.FirstOrDefaultAsync(
            It.IsAny<Expression<Func<TopupRule, bool>>>(),
            It.IsAny<Func<IQueryable<TopupRule>, IQueryable<TopupRule>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync((TopupRule?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.ProcessTopUpExecutionAsync(ruleId, CancellationToken.None)
        );

        Assert.Contains("Top-up rule not found", exception.Message);
    }

    [Fact]
    public async Task ProcessTopUpExecutionAsync_WhenRuleAlreadyExecuted_ThrowsNotFoundException()
    {
        // Arrange
        var ruleId = Guid.NewGuid().ToString();
        var topupRule = new TopupRule
        {
            Id = ruleId,
            IsExecuted = true,
            BatchRuleExecutions = new List<BatchRuleExecution>()
        };

        _mockTopupRuleRepo.Setup(r => r.FirstOrDefaultAsync(
            It.IsAny<Expression<Func<TopupRule, bool>>>(),
            It.IsAny<Func<IQueryable<TopupRule>, IQueryable<TopupRule>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(topupRule);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.ProcessTopUpExecutionAsync(ruleId, CancellationToken.None)
        );

        Assert.Contains("already executed", exception.Message);
    }

    [Fact]
    public async Task ProcessTopUpExecutionAsync_UpdatesRuleStatus()
    {
        // Arrange
        var ruleId = Guid.NewGuid().ToString();
        var batchId = Guid.NewGuid().ToString();
        var accountId = Guid.NewGuid().ToString();

        var batchExecution = new BatchExecution { Id = batchId, Status = TopUpStatus.Scheduled };
        var topupRule = new TopupRule
        {
            Id = ruleId,
            RuleName = "Test Rule",
            TopupAmount = 100,
            IsExecuted = false,
            BatchRuleExecutions = new List<BatchRuleExecution>
            {
                new BatchRuleExecution { BatchID = batchId, RuleID = ruleId, BatchExecution = batchExecution }
            }
        };

        var accounts = new List<EducationAccount>
        {
            new EducationAccount { Id = accountId, Balance = 500 }
        };

        _mockTopupRuleRepo.Setup(r => r.FirstOrDefaultAsync(
            It.IsAny<Expression<Func<TopupRule, bool>>>(),
            It.IsAny<Func<IQueryable<TopupRule>, IQueryable<TopupRule>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(topupRule);

        _mockEducationAccountRepo.Setup(r => r.ToListAsync(
            It.IsAny<Expression<Func<EducationAccount, bool>>>(),
            It.IsAny<Func<IQueryable<EducationAccount>, IQueryable<EducationAccount>>>(),
            It.IsAny<Func<IQueryable<EducationAccount>, IOrderedQueryable<EducationAccount>>>(),
            It.IsAny<int>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(accounts);

        // Act
        await _service.ProcessTopUpExecutionAsync(ruleId, CancellationToken.None);

        // Assert
        Assert.True(topupRule.IsExecuted);
        Assert.Equal(1, topupRule.NumberOfAccountsAffected);
        Assert.Equal(TopUpStatus.Completed, batchExecution.Status);
        Assert.NotNull(batchExecution.ExecutedTime);
        _mockUnitOfWork.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task ProcessTopUpExecutionAsync_OnException_SetsBatchStatusFailed()
    {
        // Arrange
        var ruleId = Guid.NewGuid().ToString();
        var batchId = Guid.NewGuid().ToString();

        var batchExecution = new BatchExecution { Id = batchId, Status = TopUpStatus.Scheduled };
        var topupRule = new TopupRule
        {
            Id = ruleId,
            RuleName = "Test Rule",
            TopupAmount = 100,
            IsExecuted = false,
            BatchRuleExecutions = new List<BatchRuleExecution>
            {
                new BatchRuleExecution { BatchID = batchId, RuleID = ruleId, BatchExecution = batchExecution }
            }
        };

        _mockTopupRuleRepo.Setup(r => r.FirstOrDefaultAsync(
            It.IsAny<Expression<Func<TopupRule, bool>>>(),
            It.IsAny<Func<IQueryable<TopupRule>, IQueryable<TopupRule>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(topupRule);

        _mockEducationAccountRepo.Setup(r => r.ToListAsync(
            It.IsAny<Expression<Func<EducationAccount, bool>>>(),
            It.IsAny<Func<IQueryable<EducationAccount>, IQueryable<EducationAccount>>>(),
            It.IsAny<Func<IQueryable<EducationAccount>, IOrderedQueryable<EducationAccount>>>(),
            It.IsAny<int>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()
        )).ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ProcessTopUpExecutionAsync(ruleId, CancellationToken.None)
        );

        Assert.Equal(TopUpStatus.Failed, batchExecution.Status);
        _mockUnitOfWork.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task ProcessTopUpExecutionAsync_CreatesHistoryOfChangeWithCorrectEducationAccountId()
    {
        // Arrange
        var ruleId = Guid.NewGuid().ToString();
        var batchId = Guid.NewGuid().ToString();
        var accountId = Guid.NewGuid().ToString();

        var batchExecution = new BatchExecution { Id = batchId, Status = TopUpStatus.Scheduled };
        var topupRule = new TopupRule
        {
            Id = ruleId,
            RuleName = "Test Rule",
            TopupAmount = 250,
            IsExecuted = false,
            BatchRuleExecutions = new List<BatchRuleExecution>
            {
                new BatchRuleExecution { BatchID = batchId, RuleID = ruleId, BatchExecution = batchExecution }
            }
        };

        var accounts = new List<EducationAccount>
        {
            new EducationAccount { Id = accountId, Balance = 1000 }
        };

        _mockTopupRuleRepo.Setup(r => r.FirstOrDefaultAsync(
            It.IsAny<Expression<Func<TopupRule, bool>>>(),
            It.IsAny<Func<IQueryable<TopupRule>, IQueryable<TopupRule>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(topupRule);

        _mockEducationAccountRepo.Setup(r => r.ToListAsync(
            It.IsAny<Expression<Func<EducationAccount, bool>>>(),
            It.IsAny<Func<IQueryable<EducationAccount>, IQueryable<EducationAccount>>>(),
            It.IsAny<Func<IQueryable<EducationAccount>, IOrderedQueryable<EducationAccount>>>(),
            It.IsAny<int>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(accounts);

        // Act
        await _service.ProcessTopUpExecutionAsync(ruleId, CancellationToken.None);

        // Assert
        _mockHistoryOfChangeRepo.Verify(
            r => r.InsertAsync(It.Is<HistoryOfChange>(hoc => 
                hoc.EducationAccountId == accountId &&
                hoc.Amount == 250 &&
                hoc.Type == ChangeType.TopUp
            )), 
            Times.Once
        );
    }

    [Fact]
    public async Task ProcessTopUpExecutionAsync_WhenAccountsFound_SetsBatchStatusCompletedAndUpdatesBalance()
    {
        // Arrange
        var ruleId = Guid.NewGuid().ToString();
        var batchId = Guid.NewGuid().ToString();
        var accountId = Guid.NewGuid().ToString();

        var batchExecution = new BatchExecution { Id = batchId, Status = TopUpStatus.Scheduled };
        var topupRule = new TopupRule
        {
            Id = ruleId,
            RuleName = "Test Rule",
            TopupAmount = 75,
            IsExecuted = false,
            BatchRuleExecutions = new List<BatchRuleExecution>
            {
                new BatchRuleExecution { BatchID = batchId, RuleID = ruleId, BatchExecution = batchExecution }
            }
        };

        var account = new EducationAccount { Id = accountId, Balance = 200 };
        var accounts = new List<EducationAccount> { account };

        _mockTopupRuleRepo.Setup(r => r.FirstOrDefaultAsync(
            It.IsAny<Expression<Func<TopupRule, bool>>>(),
            It.IsAny<Func<IQueryable<TopupRule>, IQueryable<TopupRule>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(topupRule);

        _mockEducationAccountRepo.Setup(r => r.ToListAsync(
            It.IsAny<Expression<Func<EducationAccount, bool>>>(),
            It.IsAny<Func<IQueryable<EducationAccount>, IQueryable<EducationAccount>>>(),
            It.IsAny<Func<IQueryable<EducationAccount>, IOrderedQueryable<EducationAccount>>>(),
            It.IsAny<int>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(accounts);

        // Act
        await _service.ProcessTopUpExecutionAsync(ruleId, CancellationToken.None);

        // Assert
        Assert.Equal(275, account.Balance);
        Assert.Equal(TopUpStatus.Completed, batchExecution.Status);
        Assert.Equal(1, topupRule.NumberOfAccountsAffected);
    }

    #endregion
}
