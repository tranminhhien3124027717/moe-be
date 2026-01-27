using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using MOE_System.Application.Common;
using MOE_System.Application.Common.Interfaces;
using MOE_System.Application.Interfaces.Services;
using MOE_System.Domain.Entities;
using static MOE_System.Domain.Common.BaseException;

namespace MOE_System.Application.Services;

public class EducationAccountService : IEducationAccountService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly AccountClosureOptions _options;

    public EducationAccountService(IUnitOfWork unitOfWork, IClock clock, IOptions<AccountClosureOptions> options)
    {
        _unitOfWork = unitOfWork;
        _clock = clock;
        _options = options.Value;
    }

    public async Task CloseEducationAccountManuallyAsync(string educationAccountId, CancellationToken cancellationToken)
    {
        var educationAccountRepository = _unitOfWork.GetRepository<EducationAccount>();

        var educationAccount = await educationAccountRepository.FirstOrDefaultAsync(
            predicate: ea => ea.Id == educationAccountId,
            include: query => query.Include(ea => ea.AccountHolder),
            asTracking: true,
            cancellationToken: cancellationToken
        );

        if (educationAccount == null)
        {
            throw new NotFoundException("Education account holder not found.");
        }

        educationAccount.CloseAccount();

        await _unitOfWork.SaveAsync();
    }

    public async Task AutoCloseEducationAccountsAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var today = _clock.TodayInTimeZone(_options.TimeZone);

        var scheduledDate = new DateOnly(today.Year, _options.ProcessingMonth, _options.ProcessingDay);

        if (today < scheduledDate)
        {
            return;
        }

        var maxBirthYear = today.Year - _options.AgeThreshold;

        var educationAccountRepository = _unitOfWork.GetRepository<EducationAccount>();

        var educationAccounts = await educationAccountRepository.ToListAsync(
            predicate: 
                ea => ea.IsActive && 
                ea.ClosedDate == null &&
                ea.AccountHolder!.DateOfBirth.Year <= maxBirthYear,
            include: query => query.Include(ea => ea.AccountHolder),
            asTracking: true,
            cancellationToken: cancellationToken
        );

        if (!educationAccounts.Any()) return;

        foreach (var educationAccount in educationAccounts)
        {
            educationAccount.CloseAccount();
        }

        await _unitOfWork.SaveAsync();
    }
}