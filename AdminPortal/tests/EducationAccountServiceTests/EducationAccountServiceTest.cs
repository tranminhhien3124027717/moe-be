using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using MOE_System.Application.Common;
using MOE_System.Application.Common.Interfaces;
using MOE_System.Application.Interfaces.Services;
using MOE_System.Application.Services;
using MOE_System.Domain.Entities;
using MOE_System.Domain.Enums;
using Xunit;
using static MOE_System.Domain.Common.BaseException;

namespace MOE_System.Application.Tests.EducationAccountServiceTests;

public class EducationAccountServiceTest
{
    #region Setup and Mocks

    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGenericRepository<EducationAccount>> _educationAccountRepositoryMock;
    private readonly Mock<IClock> _clockMock;
    private readonly Mock<IOptions<AccountClosureOptions>> _optionsMock;

    public EducationAccountServiceTest()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _educationAccountRepositoryMock = new Mock<IGenericRepository<EducationAccount>>();
        _clockMock = new Mock<IClock>();
        _optionsMock = new Mock<IOptions<AccountClosureOptions>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<EducationAccount>())
            .Returns(_educationAccountRepositoryMock.Object);
    }

    private IEducationAccountService CreateService(AccountClosureOptions options, DateOnly? today = null)
    {
        var optionsMock = new Mock<IOptions<AccountClosureOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);

        var clockMock = new Mock<IClock>();
        clockMock.Setup(c => c.TodayInTimeZone(It.IsAny<string>()))
            .Returns(today ?? new DateOnly(2026, 1, 15));

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var educationAccountRepoMock = new Mock<IGenericRepository<EducationAccount>>();
        unitOfWorkMock.Setup(u => u.GetRepository<EducationAccount>())
            .Returns(educationAccountRepoMock.Object);

        return new EducationAccountService(unitOfWorkMock.Object, clockMock.Object, optionsMock.Object);
    }

    private EducationAccount CreateEducationAccount(
        string? id = null,
        bool isActive = true,
        DateTime? closedDate = null,
        AccountHolder? accountHolder = null)
    {
        return new EducationAccount
        {
            Id = id ?? Guid.NewGuid().ToString(),
            UserName = "testuser",
            Password = "hashedpassword",
            Balance = 500m,
            IsActive = isActive,
            ClosedDate = closedDate,
            AccountHolderId = accountHolder?.Id ?? Guid.NewGuid().ToString(),
            AccountHolder = accountHolder,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow.AddDays(-1)
        };
    }

    private AccountHolder CreateAccountHolder(
        string? nric = null,
        DateTime? dateOfBirth = null,
        EducationAccount? educationAccount = null)
    {
        var holder = new AccountHolder
        {
            Id = Guid.NewGuid().ToString(),
            FirstName = "John",
            LastName = "Doe",
            NRIC = nric ?? "S1234567A",
            DateOfBirth = dateOfBirth ?? new DateTime(2000, 1, 15),
            Email = "john.doe@email.com",
            ContactNumber = "91234567",
            SchoolingStatus = SchoolingStatus.InSchool,
            EducationLevel = EducationLevel.Secondary,
            CreatedAt = DateTime.UtcNow
        };

        if (educationAccount != null)
        {
            educationAccount.AccountHolder = holder;
            educationAccount.AccountHolderId = holder.Id;
            holder.EducationAccount = educationAccount;
        }

        return holder;
    }

    #endregion

    #region CloseEducationAccountManuallyAsync Tests

    [Fact]
    public async Task CloseEducationAccountManuallyAsync_WithValidAccountId_ClosesAccount()
    {
        // Arrange
        var accountId = Guid.NewGuid().ToString();
        var holder = CreateAccountHolder();
        var account = CreateEducationAccount(id: accountId, isActive: true, accountHolder: holder);

        var options = new AccountClosureOptions
        {
            Enabled = true,
            AgeThreshold = 30,
            ProcessingDay = 1,
            ProcessingMonth = 1,
            TimeZone = "Singapore Standard Time"
        };
        var service = CreateService(options);

        var educationAccountRepoMock = new Mock<IGenericRepository<EducationAccount>>();
        educationAccountRepoMock
            .Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<EducationAccount, bool>>>(),
                It.IsAny<Func<IQueryable<EducationAccount>, IQueryable<EducationAccount>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetRepository<EducationAccount>())
            .Returns(educationAccountRepoMock.Object);
        unitOfWorkMock.Setup(u => u.SaveAsync()).Returns(Task.CompletedTask);

        var optionsMock = new Mock<IOptions<AccountClosureOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        var clockMock = new Mock<IClock>();
        clockMock.Setup(c => c.TodayInTimeZone(It.IsAny<string>())).Returns(new DateOnly(2026, 1, 15));

        service = new EducationAccountService(unitOfWorkMock.Object, clockMock.Object, optionsMock.Object);

        // Act
        await service.CloseEducationAccountManuallyAsync(accountId, CancellationToken.None);

        // Assert
        account.IsActive.Should().BeFalse();
        account.ClosedDate.Should().NotBeNull();
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task CloseEducationAccountManuallyAsync_WithInvalidAccountId_ThrowsNotFoundException()
    {
        // Arrange
        var accountId = Guid.NewGuid().ToString();

        var options = new AccountClosureOptions
        {
            Enabled = true,
            AgeThreshold = 30,
            ProcessingDay = 1,
            ProcessingMonth = 1,
            TimeZone = "Singapore Standard Time"
        };

        var educationAccountRepoMock = new Mock<IGenericRepository<EducationAccount>>();
        educationAccountRepoMock
            .Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<EducationAccount, bool>>>(),
                It.IsAny<Func<IQueryable<EducationAccount>, IQueryable<EducationAccount>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((EducationAccount?)null);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetRepository<EducationAccount>())
            .Returns(educationAccountRepoMock.Object);

        var optionsMock = new Mock<IOptions<AccountClosureOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        var clockMock = new Mock<IClock>();
        clockMock.Setup(c => c.TodayInTimeZone(It.IsAny<string>())).Returns(new DateOnly(2026, 1, 15));

        var service = new EducationAccountService(unitOfWorkMock.Object, clockMock.Object, optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => service.CloseEducationAccountManuallyAsync(accountId, CancellationToken.None));

        exception.Message.Should().Contain("Education account holder not found");
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Never);
    }

    [Fact]
    public async Task CloseEducationAccountManuallyAsync_WithAlreadyClosedAccount_SetClosedDateAgain()
    {
        // Arrange
        var accountId = Guid.NewGuid().ToString();
        var previousClosedDate = DateTime.UtcNow.AddDays(-30);
        var holder = CreateAccountHolder();
        var account = CreateEducationAccount(id: accountId, isActive: false, closedDate: previousClosedDate, accountHolder: holder);

        var options = new AccountClosureOptions
        {
            Enabled = true,
            AgeThreshold = 30,
            ProcessingDay = 1,
            ProcessingMonth = 1,
            TimeZone = "Singapore Standard Time"
        };

        var educationAccountRepoMock = new Mock<IGenericRepository<EducationAccount>>();
        educationAccountRepoMock
            .Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<EducationAccount, bool>>>(),
                It.IsAny<Func<IQueryable<EducationAccount>, IQueryable<EducationAccount>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetRepository<EducationAccount>())
            .Returns(educationAccountRepoMock.Object);
        unitOfWorkMock.Setup(u => u.SaveAsync()).Returns(Task.CompletedTask);

        var optionsMock = new Mock<IOptions<AccountClosureOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        var clockMock = new Mock<IClock>();
        clockMock.Setup(c => c.TodayInTimeZone(It.IsAny<string>())).Returns(new DateOnly(2026, 1, 15));

        var service = new EducationAccountService(unitOfWorkMock.Object, clockMock.Object, optionsMock.Object);

        // Act
        await service.CloseEducationAccountManuallyAsync(accountId, CancellationToken.None);

        // Assert
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task CloseEducationAccountManuallyAsync_WithCancellationToken_CancelsOperation()
    {
        // Arrange
        var accountId = Guid.NewGuid().ToString();
        var cancellationToken = new CancellationToken(canceled: true);

        var options = new AccountClosureOptions
        {
            Enabled = true,
            AgeThreshold = 30,
            ProcessingDay = 1,
            ProcessingMonth = 1,
            TimeZone = "Singapore Standard Time"
        };

        var educationAccountRepoMock = new Mock<IGenericRepository<EducationAccount>>();
        educationAccountRepoMock
            .Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<EducationAccount, bool>>>(),
                It.IsAny<Func<IQueryable<EducationAccount>, IQueryable<EducationAccount>>>(),
                It.IsAny<bool>(),
                cancellationToken))
            .ThrowsAsync(new OperationCanceledException());

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetRepository<EducationAccount>())
            .Returns(educationAccountRepoMock.Object);

        var optionsMock = new Mock<IOptions<AccountClosureOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        var clockMock = new Mock<IClock>();

        var service = new EducationAccountService(unitOfWorkMock.Object, clockMock.Object, optionsMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.CloseEducationAccountManuallyAsync(accountId, cancellationToken));
    }

    #endregion

    #region AutoCloseEducationAccountsAsync Tests

    [Fact]
    public async Task AutoCloseEducationAccountsAsync_WhenDisabled_DoesNothing()
    {
        // Arrange
        var options = new AccountClosureOptions
        {
            Enabled = false,
            AgeThreshold = 30,
            ProcessingDay = 1,
            ProcessingMonth = 1,
            TimeZone = "Singapore Standard Time"
        };

        var educationAccountRepoMock = new Mock<IGenericRepository<EducationAccount>>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetRepository<EducationAccount>())
            .Returns(educationAccountRepoMock.Object);

        var optionsMock = new Mock<IOptions<AccountClosureOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        var clockMock = new Mock<IClock>();
        clockMock.Setup(c => c.TodayInTimeZone(It.IsAny<string>())).Returns(new DateOnly(2026, 1, 15));

        var service = new EducationAccountService(unitOfWorkMock.Object, clockMock.Object, optionsMock.Object);

        // Act
        await service.AutoCloseEducationAccountsAsync(CancellationToken.None);

        // Assert
        educationAccountRepoMock.Verify(
            r => r.ToListAsync(
                It.IsAny<Expression<Func<EducationAccount, bool>>>(),
                It.IsAny<Func<IQueryable<EducationAccount>, IQueryable<EducationAccount>>>(),
                It.IsAny<Func<IQueryable<EducationAccount>, IOrderedQueryable<EducationAccount>>>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Never);
    }

    [Fact]
    public async Task AutoCloseEducationAccountsAsync_WhenBeforeScheduledDate_DoesNothing()
    {
        // Arrange
        var today = new DateOnly(2026, 1, 10);
        var options = new AccountClosureOptions
        {
            Enabled = true,
            AgeThreshold = 30,
            ProcessingDay = 15,
            ProcessingMonth = 1,
            TimeZone = "Singapore Standard Time"
        };

        var educationAccountRepoMock = new Mock<IGenericRepository<EducationAccount>>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetRepository<EducationAccount>())
            .Returns(educationAccountRepoMock.Object);

        var optionsMock = new Mock<IOptions<AccountClosureOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        var clockMock = new Mock<IClock>();
        clockMock.Setup(c => c.TodayInTimeZone(It.IsAny<string>())).Returns(today);

        var service = new EducationAccountService(unitOfWorkMock.Object, clockMock.Object, optionsMock.Object);

        // Act
        await service.AutoCloseEducationAccountsAsync(CancellationToken.None);

        // Assert
        educationAccountRepoMock.Verify(
            r => r.ToListAsync(
                It.IsAny<Expression<Func<EducationAccount, bool>>>(),
                It.IsAny<Func<IQueryable<EducationAccount>, IQueryable<EducationAccount>>>(),
                It.IsAny<Func<IQueryable<EducationAccount>, IOrderedQueryable<EducationAccount>>>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Never);
    }

    [Fact]
    public async Task AutoCloseEducationAccountsAsync_WhenScheduledDateArrived_ProcessesEligibleAccounts()
    {
        // Arrange
        var today = new DateOnly(2026, 1, 15);
        // Eligible accounts: BirthYear <= 1996 (age 30+) when maxBirthYear = 2026 - 30
        var options = new AccountClosureOptions
        {
            Enabled = true,
            AgeThreshold = 30,
            ProcessingDay = 15,
            ProcessingMonth = 1,
            TimeZone = "Singapore Standard Time"
        };

        // Eligible: born in 1996, BirthYear <= 1996 (age 30)
        var eligibleHolder = CreateAccountHolder(dateOfBirth: new DateTime(1996, 1, 15));
        var eligibleAccount = CreateEducationAccount(isActive: true, accountHolder: eligibleHolder);

        // Ineligible: born in 1997, BirthYear > 1996 (age 29)
        var ineligibleHolder = CreateAccountHolder(dateOfBirth: new DateTime(1997, 1, 15));
        var ineligibleAccount = CreateEducationAccount(isActive: true, accountHolder: ineligibleHolder);

        // Query should only return eligible and active accounts
        var accounts = new List<EducationAccount> { eligibleAccount };

        var educationAccountRepoMock = new Mock<IGenericRepository<EducationAccount>>();
        educationAccountRepoMock
            .Setup(r => r.ToListAsync(
                It.IsAny<Expression<Func<EducationAccount, bool>>>(),
                It.IsAny<Func<IQueryable<EducationAccount>, IQueryable<EducationAccount>>>(),
                It.IsAny<Func<IQueryable<EducationAccount>, IOrderedQueryable<EducationAccount>>>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetRepository<EducationAccount>())
            .Returns(educationAccountRepoMock.Object);
        unitOfWorkMock.Setup(u => u.SaveAsync()).Returns(Task.CompletedTask);

        var optionsMock = new Mock<IOptions<AccountClosureOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        var clockMock = new Mock<IClock>();
        clockMock.Setup(c => c.TodayInTimeZone(It.IsAny<string>())).Returns(today);

        var service = new EducationAccountService(unitOfWorkMock.Object, clockMock.Object, optionsMock.Object);

        // Act
        await service.AutoCloseEducationAccountsAsync(CancellationToken.None);

        // Assert
        eligibleAccount.IsActive.Should().BeFalse();
        eligibleAccount.ClosedDate.Should().NotBeNull();
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task AutoCloseEducationAccountsAsync_WhenNoEligibleAccounts_DoesNothing()
    {
        // Arrange
        var today = new DateOnly(2026, 1, 15);
        var options = new AccountClosureOptions
        {
            Enabled = true,
            AgeThreshold = 30,
            ProcessingDay = 15,
            ProcessingMonth = 1,
            TimeZone = "Singapore Standard Time"
        };

        // Predicate filters out ineligible accounts, so empty list is returned
        var educationAccountRepoMock = new Mock<IGenericRepository<EducationAccount>>();
        educationAccountRepoMock
            .Setup(r => r.ToListAsync(
                It.IsAny<Expression<Func<EducationAccount, bool>>>(),
                It.IsAny<Func<IQueryable<EducationAccount>, IQueryable<EducationAccount>>>(),
                It.IsAny<Func<IQueryable<EducationAccount>, IOrderedQueryable<EducationAccount>>>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EducationAccount>());

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetRepository<EducationAccount>())
            .Returns(educationAccountRepoMock.Object);
        unitOfWorkMock.Setup(u => u.SaveAsync()).Returns(Task.CompletedTask);

        var optionsMock = new Mock<IOptions<AccountClosureOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        var clockMock = new Mock<IClock>();
        clockMock.Setup(c => c.TodayInTimeZone(It.IsAny<string>())).Returns(today);

        var service = new EducationAccountService(unitOfWorkMock.Object, clockMock.Object, optionsMock.Object);

        // Act
        await service.AutoCloseEducationAccountsAsync(CancellationToken.None);

        // Assert - Returns early if no accounts found, does not call SaveAsync
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Never);
    }

    [Fact]
    public async Task AutoCloseEducationAccountsAsync_WithMultipleEligibleAccounts_CloseAll()
    {
        // Arrange
        var today = new DateOnly(2026, 1, 15);
        // Eligible accounts: BirthYear <= 1996 (age 30+) when maxBirthYear = 2026 - 30
        var options = new AccountClosureOptions
        {
            Enabled = true,
            AgeThreshold = 30,
            ProcessingDay = 15,
            ProcessingMonth = 1,
            TimeZone = "Singapore Standard Time"
        };

        var accounts = new List<EducationAccount>();
        // Create 5 accounts all aged 30+ (BirthYear <= 1996)
        for (int i = 0; i < 5; i++)
        {
            var holder = CreateAccountHolder(dateOfBirth: new DateTime(1996 - i, 1, 15));
            accounts.Add(CreateEducationAccount(isActive: true, accountHolder: holder));
        }

        var educationAccountRepoMock = new Mock<IGenericRepository<EducationAccount>>();
        educationAccountRepoMock
            .Setup(r => r.ToListAsync(
                It.IsAny<Expression<Func<EducationAccount, bool>>>(),
                It.IsAny<Func<IQueryable<EducationAccount>, IQueryable<EducationAccount>>>(),
                It.IsAny<Func<IQueryable<EducationAccount>, IOrderedQueryable<EducationAccount>>>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetRepository<EducationAccount>())
            .Returns(educationAccountRepoMock.Object);
        unitOfWorkMock.Setup(u => u.SaveAsync()).Returns(Task.CompletedTask);

        var optionsMock = new Mock<IOptions<AccountClosureOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        var clockMock = new Mock<IClock>();
        clockMock.Setup(c => c.TodayInTimeZone(It.IsAny<string>())).Returns(today);

        var service = new EducationAccountService(unitOfWorkMock.Object, clockMock.Object, optionsMock.Object);

        // Act
        await service.AutoCloseEducationAccountsAsync(CancellationToken.None);

        // Assert
        foreach (var account in accounts)
        {
            account.IsActive.Should().BeFalse();
            account.ClosedDate.Should().NotBeNull();
        }
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task AutoCloseEducationAccountsAsync_WithEmptyList_SavesWithoutProcessing()
    {
        // Arrange
        var today = new DateOnly(2026, 1, 15);
        var options = new AccountClosureOptions
        {
            Enabled = true,
            AgeThreshold = 30,
            ProcessingDay = 15,
            ProcessingMonth = 1,
            TimeZone = "Singapore Standard Time"
        };

        var educationAccountRepoMock = new Mock<IGenericRepository<EducationAccount>>();
        educationAccountRepoMock
            .Setup(r => r.ToListAsync(
                It.IsAny<Expression<Func<EducationAccount, bool>>>(),
                It.IsAny<Func<IQueryable<EducationAccount>, IQueryable<EducationAccount>>>(),
                It.IsAny<Func<IQueryable<EducationAccount>, IOrderedQueryable<EducationAccount>>>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EducationAccount>());

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetRepository<EducationAccount>())
            .Returns(educationAccountRepoMock.Object);
        unitOfWorkMock.Setup(u => u.SaveAsync()).Returns(Task.CompletedTask);

        var optionsMock = new Mock<IOptions<AccountClosureOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        var clockMock = new Mock<IClock>();
        clockMock.Setup(c => c.TodayInTimeZone(It.IsAny<string>())).Returns(today);

        var service = new EducationAccountService(unitOfWorkMock.Object, clockMock.Object, optionsMock.Object);

        // Act
        await service.AutoCloseEducationAccountsAsync(CancellationToken.None);

        // Assert - Returns early if no accounts found, does not call SaveAsync
    }

    [Fact]
    public async Task AutoCloseEducationAccountsAsync_OnScheduledDate_ProcessesCorrectly()
    {
        // Arrange
        var today = new DateOnly(2026, 1, 15);
        // Eligible accounts: BirthYear <= 1996 (age 30+) when maxBirthYear = 2026 - 30
        var options = new AccountClosureOptions
        {
            Enabled = true,
            AgeThreshold = 30,
            ProcessingDay = 15,
            ProcessingMonth = 1,
            TimeZone = "Singapore Standard Time"
        };

        // Born in 1996, BirthYear = 1996 <= 1996 - eligible
        var holder = CreateAccountHolder(dateOfBirth: new DateTime(1996, 1, 15));
        var account = CreateEducationAccount(isActive: true, accountHolder: holder);

        var educationAccountRepoMock = new Mock<IGenericRepository<EducationAccount>>();
        educationAccountRepoMock
            .Setup(r => r.ToListAsync(
                It.IsAny<Expression<Func<EducationAccount, bool>>>(),
                It.IsAny<Func<IQueryable<EducationAccount>, IQueryable<EducationAccount>>>(),
                It.IsAny<Func<IQueryable<EducationAccount>, IOrderedQueryable<EducationAccount>>>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EducationAccount> { account });

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetRepository<EducationAccount>())
            .Returns(educationAccountRepoMock.Object);
        unitOfWorkMock.Setup(u => u.SaveAsync()).Returns(Task.CompletedTask);

        var optionsMock = new Mock<IOptions<AccountClosureOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        var clockMock = new Mock<IClock>();
        clockMock.Setup(c => c.TodayInTimeZone(It.IsAny<string>())).Returns(today);

        var service = new EducationAccountService(unitOfWorkMock.Object, clockMock.Object, optionsMock.Object);

        // Act
        await service.AutoCloseEducationAccountsAsync(CancellationToken.None);

        // Assert
        account.IsActive.Should().BeFalse();
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task AutoCloseEducationAccountsAsync_WithMixedStates_OnlyClosesEligibleActive()
    {
        // Arrange
        var today = new DateOnly(2026, 1, 15);
        // Eligible accounts: BirthYear <= 1996 (age 30+) when maxBirthYear = 2026 - 30
        var options = new AccountClosureOptions
        {
            Enabled = true,
            AgeThreshold = 30,
            ProcessingDay = 15,
            ProcessingMonth = 1,
            TimeZone = "Singapore Standard Time"
        };

        // Eligible and active - should close (born 1996, BirthYear <= 1996, isActive=true, ClosedDate=null)
        var activeEligibleHolder = CreateAccountHolder(dateOfBirth: new DateTime(1996, 1, 15));
        var activeEligible = CreateEducationAccount(isActive: true, accountHolder: activeEligibleHolder);

        // Inactive - filtered out by predicate (isActive=false)
        var inactiveEligibleHolder = CreateAccountHolder(dateOfBirth: new DateTime(1995, 6, 15));
        var inactiveEligible = CreateEducationAccount(isActive: false, closedDate: DateTime.UtcNow.AddDays(-10), accountHolder: inactiveEligibleHolder);

        // Eligible and active - should close (born 1996, BirthYear <= 1996, isActive=true, ClosedDate=null)
        var activeWithNullClosedHolder = CreateAccountHolder(dateOfBirth: new DateTime(1996, 3, 15));
        var activeWithNullClosed = CreateEducationAccount(isActive: true, closedDate: null, accountHolder: activeWithNullClosedHolder);

        // Query predicate filters: isActive=true AND ClosedDate=null AND BirthYear <= 1996
        var accounts = new List<EducationAccount> { activeEligible, activeWithNullClosed };

        var educationAccountRepoMock = new Mock<IGenericRepository<EducationAccount>>();
        educationAccountRepoMock
            .Setup(r => r.ToListAsync(
                It.IsAny<Expression<Func<EducationAccount, bool>>>(),
                It.IsAny<Func<IQueryable<EducationAccount>, IQueryable<EducationAccount>>>(),
                It.IsAny<Func<IQueryable<EducationAccount>, IOrderedQueryable<EducationAccount>>>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.GetRepository<EducationAccount>())
            .Returns(educationAccountRepoMock.Object);
        unitOfWorkMock.Setup(u => u.SaveAsync()).Returns(Task.CompletedTask);

        var optionsMock = new Mock<IOptions<AccountClosureOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        var clockMock = new Mock<IClock>();
        clockMock.Setup(c => c.TodayInTimeZone(It.IsAny<string>())).Returns(today);

        var service = new EducationAccountService(unitOfWorkMock.Object, clockMock.Object, optionsMock.Object);

        // Act
        await service.AutoCloseEducationAccountsAsync(CancellationToken.None);

        // Assert
        activeEligible.IsActive.Should().BeFalse();
        activeWithNullClosed.IsActive.Should().BeFalse();
        unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
    }

    #endregion
}

