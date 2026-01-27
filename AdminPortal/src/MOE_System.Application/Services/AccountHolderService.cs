using Microsoft.EntityFrameworkCore;
using MOE_System.Application.Common;
using MOE_System.Application.Common.Interfaces;
using MOE_System.Application.DTOs;
using MOE_System.Application.DTOs.AccountHolder;
using MOE_System.Application.DTOs.AccountHolder.Request;
using MOE_System.Application.DTOs.AccountHolder.Response;
using MOE_System.Application.Interfaces;
using MOE_System.Domain.Entities;
using MOE_System.Domain.Enums;
using System.Text.RegularExpressions;
using System.Linq;
using static MOE_System.Domain.Common.BaseException;

namespace MOE_System.Application.Services;

public class AccountHolderService : IAccountHolderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;

    public AccountHolderService(IUnitOfWork unitOfWork, IPasswordService passwordService)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
    }

    public async Task<ResidentInfoResponse> GetResidentAccountHolderByNRICAsync(string nric)
    {
        var residentRepo = _unitOfWork.GetRepository<Resident>();
        
        var resident = await residentRepo.Entities
            .FirstOrDefaultAsync(ah => ah.NRIC == nric);
        if(resident == null)
        {
            throw new NotFoundException("RESIDENT_NOT_FOUND", $"Account holder with NRIC {nric} not found.");
        }
        return new ResidentInfoResponse
        {
            FullName = resident.PrincipalName,
            DateOfBirth = resident.DateOfBirth,
            Email = resident.EmailAddress,
            PhoneNumber = resident.MobileNumber,
            RegisteredAddress = resident.RegisteredAddress,
            ResidentialStatus = resident.ResidentialStatus
        };
    }

    public async Task<AccountHolderDetailResponse> GetAccountHolderDetailAsync(string accountHolderId)
    {
        var accountHolderRepo = _unitOfWork.GetRepository<AccountHolder>();

        var accountHolder = await accountHolderRepo.Entities
            .Include(ah => ah.EducationAccount!)
                .ThenInclude(ea => ea.Enrollments)
                    .ThenInclude(en => en.Invoices)
            .FirstOrDefaultAsync(ah => ah.Id == accountHolderId);

        if (accountHolder == null)
        {
            throw new NotFoundException("ACCOUNT_HOLDER_NOT_FOUND", $"Account holder with ID {accountHolderId} not found.");
        }

        var accountHolderDetailResponse = new AccountHolderDetailResponse
        {
            FullName = $"{accountHolder.FirstName} {accountHolder.LastName}",
            NRIC = accountHolder.NRIC,
            EducationAccountUserName = accountHolder.EducationAccount?.UserName ?? string.Empty,
            Balance = accountHolder.EducationAccount?.Balance ?? 0,
            CourseCount = accountHolder.EducationAccount?.Enrollments?.Count ?? 0,
            OutstandingFees = accountHolder.EducationAccount?.Enrollments?
                .SelectMany(e => e.Invoices)
                .Where(i => i.Status == InvoiceStatus.Outstanding)
                .Sum(i => i.Amount) ?? 0,
            IsActive = accountHolder.EducationAccount?.IsActive,

            StudentInformation = new StudentInformation
            {
                DateOfBirth = accountHolder.DateOfBirth.ToString("dd/MM/yyyy"),
                Age = DateTime.UtcNow.Year - accountHolder.DateOfBirth.Year - (DateTime.UtcNow.DayOfYear < accountHolder.DateOfBirth.DayOfYear ? 1 : 0),
                Email = accountHolder.Email,
                ContactNumber = accountHolder.ContactNumber,
                SchoolingStatus = accountHolder.SchoolingStatus.ToString(),
                EducationLevel = accountHolder.EducationLevel.ToString(),
                ResidentialStatus = accountHolder.ResidentialStatus,
                RegisteredAddress = accountHolder.RegisteredAddress,
                MailingAddress = accountHolder.MailingAddress,
                CreatedAt = accountHolder.CreatedAt.ToString("dd/MM/yyyy")
            }
        };

        return accountHolderDetailResponse;
    }

    public async Task<PaginatedList<EnrolledCourseInfo>> GetEnrolledCoursesAsync(string accountHolderId, int pageNumber = 1, int pageSize = 5)
    {
        var accountHolderRepo = _unitOfWork.GetRepository<AccountHolder>();

        var accountHolder = await accountHolderRepo.Entities
            .Include(ah => ah.EducationAccount!)
                .ThenInclude(ea => ea.Enrollments)
                    .ThenInclude(en => en.Course!)
                        .ThenInclude(c => c.Provider)
            .Include(ah => ah.EducationAccount!)
                .ThenInclude(ea => ea.Enrollments)
                    .ThenInclude(en => en.Invoices)
            .FirstOrDefaultAsync(ah => ah.Id == accountHolderId);

        if (accountHolder == null)
        {
            throw new NotFoundException("ACCOUNT_HOLDER_NOT_FOUND", $"Account holder with ID {accountHolderId} not found.");
        }

        if (accountHolder.EducationAccount?.Enrollments == null)
        {
            return new PaginatedList<EnrolledCourseInfo>(new List<EnrolledCourseInfo>(), 0, pageNumber, pageSize);
        }

        var enrolledCourses = accountHolder.EducationAccount.Enrollments
            .Select(e => new EnrolledCourseInfo
            {
                CourseId = e.Course?.Id ?? string.Empty,
                CourseName = e.Course?.CourseName ?? string.Empty,
                ProviderName = e.Course?.Provider?.Name ?? string.Empty,
                BillingCycle = e.Course?.BillingCycle ?? "-",
                TotalFree = e.Course?.FeeAmount ?? 0,
                EnrollmentDate = e.EnrollDate.ToString("dd/MM/yyyy"),
                CollectedFee = e.Invoices?.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.Amount) ?? 0,
                NextPaymentDue = GetNextBillingDate(e),
                PaymentStatus = GetPaymentStatus(e)
            }).ToList();

        var totalCount = enrolledCourses.Count;
        var paginatedItems = enrolledCourses
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginatedList<EnrolledCourseInfo>(paginatedItems, totalCount, pageNumber, pageSize);
    }

    public async Task<PaginatedList<OutstandingFeeInfo>> GetOutstandingFeesAsync(string accountHolderId, int pageNumber = 1, int pageSize = 5)
    {
        var accountHolderRepo = _unitOfWork.GetRepository<AccountHolder>();

        var accountHolder = await accountHolderRepo.Entities
            .Include(ah => ah.EducationAccount!)
                .ThenInclude(ea => ea.Enrollments)
                    .ThenInclude(en => en.Course!)
                        .ThenInclude(c => c.Provider)
            .Include(ah => ah.EducationAccount!)
                .ThenInclude(ea => ea.Enrollments)
                    .ThenInclude(en => en.Invoices)
            .FirstOrDefaultAsync(ah => ah.Id == accountHolderId);

        if (accountHolder == null)
        {
            throw new NotFoundException("ACCOUNT_HOLDER_NOT_FOUND", $"Account holder with ID {accountHolderId} not found.");
        }

        if (accountHolder.EducationAccount?.Enrollments == null)
        {
            return new PaginatedList<OutstandingFeeInfo>(new List<OutstandingFeeInfo>(), 0, pageNumber, pageSize);
        }

        var outstandingFees = accountHolder.EducationAccount.Enrollments
            .SelectMany(e => e.Invoices
                .Where(i => i.Status == InvoiceStatus.Outstanding)
                .Select(i => new OutstandingFeeInfo
                {
                    CourseName = e.Course?.CourseName ?? string.Empty,
                    ProviderName = e.Course?.Provider?.Name ?? string.Empty,
                    OutstandingAmount = i.Amount,
                    DueDate = i.DueDate?.ToString("dd/MM/yyyy"),
                    BillingDate = i.BillingDate.ToString("dd/MM/yyyy")
                }))
            .ToList();

        var totalCount = outstandingFees.Count;
        var paginatedItems = outstandingFees
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginatedList<OutstandingFeeInfo>(paginatedItems, totalCount, pageNumber, pageSize);
    }

    public async Task<PaginatedList<TopUpHistoryInfo>> GetTopUpHistoryAsync(string accountHolderId, int pageNumber = 1, int pageSize = 5)
    {
        var accountHolderRepo = _unitOfWork.GetRepository<AccountHolder>();
        var historyRepo = _unitOfWork.GetRepository<HistoryOfChange>();

        var accountHolder = await accountHolderRepo.Entities
            .Include(ah => ah.EducationAccount)
            .FirstOrDefaultAsync(ah => ah.Id == accountHolderId);

        if (accountHolder == null)
        {
            throw new NotFoundException("ACCOUNT_HOLDER_NOT_FOUND", $"Account holder with ID {accountHolderId} not found.");
        }

        if (accountHolder.EducationAccount == null)
        {
            return new PaginatedList<TopUpHistoryInfo>(new List<TopUpHistoryInfo>(), 0, pageNumber, pageSize);
        }

        var topUpQuery = historyRepo.Entities
            .Where(h => h.EducationAccountId == accountHolder.EducationAccount.Id && h.Type == ChangeType.TopUp)
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new TopUpHistoryInfo
            {
                TopUpDate = h.CreatedAt.ToString("dd/MM/yyyy"),
                TopUpTime = h.CreatedAt.ToString("HH:mm tt"),
                Amount = h.Amount,
                Reference = h.ReferenceId ?? "-",
                Description = h.Description ?? "-"
            });

        return await PaginatedList<TopUpHistoryInfo>.CreateAsync(topUpQuery, pageNumber, pageSize);
    }

    public async Task<PaginatedList<PaymentHistoryInfo>> GetPaymentHistoryAsync(string accountHolderId, int pageNumber = 1, int pageSize = 5)
    {
        var accountHolderRepo = _unitOfWork.GetRepository<AccountHolder>();
        var transactionRepo = _unitOfWork.GetRepository<Transaction>();

        var accountHolder = await accountHolderRepo.Entities
            .Include(ah => ah.EducationAccount)
            .FirstOrDefaultAsync(ah => ah.Id == accountHolderId);

        if (accountHolder == null)
        {
            throw new NotFoundException("ACCOUNT_HOLDER_NOT_FOUND", $"Account holder with ID {accountHolderId} not found.");
        }

        if (accountHolder.EducationAccount == null)
        {
            return new PaginatedList<PaymentHistoryInfo>(new List<PaymentHistoryInfo>(), 0, pageNumber, pageSize);
        }

        var paymentQuery = transactionRepo.Entities
            .Where(t => t.Invoice!.Enrollment!.EducationAccountId == accountHolder.EducationAccount.Id && t.Status == TransactionStatus.Success)
            .OrderByDescending(t => t.TransactionAt)
            .Select(t => new PaymentHistoryInfo
            {
                CourseName = t.Invoice!.Enrollment!.Course!.CourseName ?? string.Empty,
                ProviderName = t.Invoice!.Enrollment!.Course!.Provider!.Name ?? string.Empty,
                PaymentDate = t.TransactionAt != null ? t.TransactionAt.Value.ToString("dd/MM/yyyy") : "-",
                AmountPaid = t.Amount,
                PaymentMethod = t.PaymentMethod.ToString()
            });

        return await PaginatedList<PaymentHistoryInfo>.CreateAsync(paymentQuery, pageNumber, pageSize);
    }

    private string GetPaymentStatus(Enrollment enrollment)
    {
        var totalFee = enrollment.Course?.FeeAmount ?? 0;
        var collectedFee = enrollment.Invoices?.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.Amount) ?? 0;
        var hasPaidInvoices = enrollment.Invoices?.Any(i => i.Status == InvoiceStatus.Paid) ?? false;
        
        // Outstanding: has at least one outstanding invoice
        var hasOutstanding = enrollment.Invoices?.Any(i => i.Status == InvoiceStatus.Outstanding) ?? false;
        if (hasOutstanding)
        {
            return "Outstanding";
        }
        
        // Fully Paid: all invoices paid and collected >= total fee
        if (collectedFee >= totalFee)
        {
            return "Fully Paid";
        }
        
        // Paid: has at least one paid invoice (but not fully paid yet)
        if (hasPaidInvoices && collectedFee < totalFee)
        {
            return "Paid";
        }
        
        // Scheduled: no invoices OR no paid invoices yet
        return "Scheduled";
    }

    private string GetNextBillingDate(Enrollment enrollment)
    {
        var paymentStatus = GetPaymentStatus(enrollment);
        var course = enrollment.Course;
        
        if (course == null || !course.BillingDate.HasValue)
        {
            return "-";
        }

        // Outstanding or Fully Paid: show "-"
        if (paymentStatus == "Outstanding" || paymentStatus == "Fully Paid")
        {
            return "-";
        }

        var currentDate = DateTime.UtcNow;
        
        // Check if we have any paid invoices
        var lastPaidInvoice = enrollment.Invoices?
            .Where(i => i.Status == InvoiceStatus.Paid)
            .OrderByDescending(i => i.BillingDate)
            .FirstOrDefault();

        if (lastPaidInvoice != null && !string.IsNullOrEmpty(course.BillingCycle))
        {
            // We have paid invoices - calculate next billing from last paid
            return CalculateNextBillingDate(lastPaidInvoice.BillingDate, course.BillingDate.Value, course.BillingCycle);
        }
        
        // No paid invoices yet - calculate first billing date
        // If course start date <= billing day in start month: bill on billing day
        // If course start date > billing day in start month: bill on start date
        var courseStartDate = course.StartDate;
        var billingDay = course.BillingDate.Value;
        
        if (courseStartDate.Day <= billingDay)
        {
            // Bill on the billing day of the start month
            var daysInMonth = DateTime.DaysInMonth(courseStartDate.Year, courseStartDate.Month);
            var actualDay = Math.Min(billingDay, daysInMonth);
            var firstBillingDate = new DateTime(courseStartDate.Year, courseStartDate.Month, actualDay);
            return firstBillingDate.ToString("dd/MM/yyyy");
        }
        else
        {
            // Bill on course start date (since it's after billing day)
            return courseStartDate.ToString("dd/MM/yyyy");
        }
    }

    private string CalculateNextBillingDate(DateTime lastBillingDate, int billingDayOfMonth, string? billingCycle)
    {
        if (string.IsNullOrEmpty(billingCycle))
        {
            return "-";
        }

        int monthsToAdd = 0;

        switch (billingCycle.ToLower())
        {
            case "monthly":
                monthsToAdd = 1;
                break;
            case "quarterly":
                monthsToAdd = 3;
                break;
            case "biannually":
                monthsToAdd = 6;
                break;
            case "annually":
            case "yearly":
                monthsToAdd = 12;
                break;
            default:
                return "-";
        }

        // Add the months to get to the next billing cycle
        var nextDate = lastBillingDate.AddMonths(monthsToAdd);

        // Set to the billing day of that month
        var daysInMonth = DateTime.DaysInMonth(nextDate.Year, nextDate.Month);
        var actualDay = Math.Min(billingDayOfMonth, daysInMonth);
        nextDate = new DateTime(nextDate.Year, nextDate.Month, actualDay);

        return nextDate.ToString("dd/MM/yyyy");
    }

    public async Task<PaginatedList<AccountHolderResponse>> GetAccountHoldersAsync(int pageNumber = 1, int pageSize = 20, AccountHolderFilterParams? filters = null)
    {
        var accountHolderRepo = _unitOfWork.GetRepository<AccountHolder>();
        
        var query = accountHolderRepo.Entities.AsQueryable();

        // Apply filters and sorting via helper methods
        query = ApplyFilters(query, filters);
        query = ApplySorting(query, filters);
        
        // Default sort by created date descending if no sort was applied
        if (filters == null || !filters.SortBy.HasValue)
        {
            query = query.OrderByDescending(ah => ah.CreatedAt);
        }

        query = query.Include(ah => ah.EducationAccount!)
                     .ThenInclude(ea => ea.Enrollments); 

        var paginatedAccountHolders = await accountHolderRepo.GetPagging(query, pageNumber, pageSize);
        
        var accountHolderResponses = paginatedAccountHolders.Items.Select(accountHolder => new AccountHolderResponse
        {
            Id = accountHolder.Id,
            FullName = $"{accountHolder.FirstName} {accountHolder.LastName}",
            NRIC = accountHolder.NRIC,
            Age = DateTime.Now.Year - accountHolder.DateOfBirth.Year,
            Balance = accountHolder.EducationAccount?.Balance ?? 0,
            EducationLevel = accountHolder.EducationLevel.ToString(),
            ResidentialStatus = accountHolder.ResidentialStatus,
            CreatedDate = DateOnly.FromDateTime(accountHolder.CreatedAt).ToString("dd/MM/yyyy"),
            CreateTime = accountHolder.CreatedAt.ToString("HH:mm tt"),
            CourseCount = accountHolder.EducationAccount?.Enrollments?.Count ?? 0,
        }).ToList();

        return new PaginatedList<AccountHolderResponse>(
            accountHolderResponses, 
            paginatedAccountHolders.TotalCount, 
            paginatedAccountHolders.PageIndex, 
            pageSize);
    }

    // Extracted filter logic
    private IQueryable<AccountHolder> ApplyFilters(IQueryable<AccountHolder> query, AccountHolderFilterParams? filters)
    {
        if (filters == null) return query;

        // Filter by IsActive status
        query = query.Where(ah => ah.EducationAccount != null && ah.EducationAccount.IsActive == filters.IsActive);

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var s = filters.Search.Trim().ToLower();
            query = query.Where(ah => (ah.FirstName + " " + ah.LastName).ToLower().Contains(s)
                                       || ah.NRIC.ToLower().Contains(s)
                                       || ah.Email.ToLower().Contains(s)
                                       || ah.ContactNumber.ToLower().Contains(s));
        }

        if (filters.EducationLevels != null && filters.EducationLevels.Any())
        {
        query = query.Where(ah => filters.EducationLevels.Contains(ah.EducationLevel));
    }

    if (filters.SchoolingStatus != null)
    {
        query = query.Where(ah => ah.SchoolingStatus == filters.SchoolingStatus.Value);
    }

    if (filters.ResidentialStatuses != null && filters.ResidentialStatuses.Any())
    {
        var residentialStatusStrings = filters.ResidentialStatuses.Select(rs => rs.ToString()).ToList();
        query = query.Where(ah => residentialStatusStrings.Contains(ah.ResidentialStatus));
    }

    if (filters.MinBalance.HasValue)
    {
            var min = filters.MinBalance.Value;
            query = query.Where(ah => ah.EducationAccount != null && ah.EducationAccount.Balance >= min);
        }

        if (filters.MaxBalance.HasValue)
        {
            var max = filters.MaxBalance.Value;
            query = query.Where(ah => ah.EducationAccount != null && ah.EducationAccount.Balance <= max);
        }

        if (filters.MinAge.HasValue || filters.MaxAge.HasValue)
        {
            var today = DateTime.Today;

            if (filters.MinAge.HasValue)
            {
                var maxDob = today.AddYears(-filters.MinAge.Value);
                query = query.Where(ah => ah.DateOfBirth <= maxDob);
            }

            if (filters.MaxAge.HasValue)
            {
                var minDob = today.AddYears(-filters.MaxAge.Value);
                query = query.Where(ah => ah.DateOfBirth >= minDob);
            }
        }

        return query;
    }

    private IQueryable<AccountHolder> ApplySorting(IQueryable<AccountHolder> query, AccountHolderFilterParams? filters)
    {
        if (filters == null || !filters.SortBy.HasValue) return query;

        var desc = filters.SortDescending == true;
        switch (filters.SortBy.Value)
        {
            case SortBy.FullName:
                query = desc
                    ? query.OrderByDescending(ah => (ah.FirstName + " " + ah.LastName).ToLower())
                    : query.OrderBy(ah => (ah.FirstName + " " + ah.LastName).ToLower());
                break;
            case SortBy.Age:
                query = desc
                    ? query.OrderBy(ah => ah.DateOfBirth)
                    : query.OrderByDescending(ah => ah.DateOfBirth);
                break;
            case SortBy.Balance:
                query = desc
                    ? query.OrderByDescending(ah => ah.EducationAccount != null ? ah.EducationAccount.Balance : 0)
                    : query.OrderBy(ah => ah.EducationAccount != null ? ah.EducationAccount.Balance : 0);
                break;
            case SortBy.EducationLevel:
                if (desc)
                {
                    query = query.OrderByDescending(ah => (int)ah.EducationLevel);
                }
                else
                {
                    query = query.OrderBy(ah => (int)ah.EducationLevel);
                }

                break;
            case SortBy.CreatedDate:
                query = desc
                    ? query.OrderByDescending(ah => ah.CreatedAt)
                    : query.OrderBy(ah => ah.CreatedAt);
                break;
            default:
                break;
        }

        return query;
    }

    public async Task<AccountHolderResponse> AddAccountHolderAsync(CreateAccountHolderRequest request)
    {
        var transaction = await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var accountHolderRepo = _unitOfWork.GetRepository<AccountHolder>();
            var educationAccountRepo = _unitOfWork.GetRepository<EducationAccount>();
            
            var isExistAccountHolder = await accountHolderRepo.Entities.FirstOrDefaultAsync(ah => ah.NRIC == request.NRIC);

            if(isExistAccountHolder != null)
            {
                throw new ValidationException("ACCOUNT_HOLDER_EXISTS", $"Account holder with NRIC {request.NRIC} already exists.");
            }

            string pattern = @"^([^\s]+)\s+(.*)$";
            Match match = Regex.Match(request.FullName, pattern);

            string firstName = string.Empty;
            string lastName = string.Empty;

            if (match.Success)
            {
                firstName = match.Groups[1].Value;
                lastName = match.Groups[2].Value;
            }

            // Create Account Holder
            var newAccountHolder = new AccountHolder
            {
                NRIC = request.NRIC,
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = request.DateOfBirth,
                Email = request.Email,
                ContactNumber = request.ContactNumber,
                RegisteredAddress = request.RegisteredAddress,
                MailingAddress = request.MailingAddress,
                SchoolingStatus = Domain.Enums.SchoolingStatus.NotInSchool,
                EducationLevel = Domain.Enums.EducationLevel.NotSet,
                ResidentialStatus = request.ResidentialStatus,
                CreatedAt = DateTime.UtcNow,
            };
            
            await accountHolderRepo.InsertAsync(newAccountHolder);
            await _unitOfWork.SaveAsync();
            
            // Create Education Account
            var newEducationAccount = new EducationAccount
            {
                AccountHolderId = newAccountHolder.Id,
                UserName = request.NRIC,
                Password = _passwordService.HashPassword(_passwordService.GenerateRandomPassword()),
                Balance = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            
            await educationAccountRepo.InsertAsync(newEducationAccount);
            await _unitOfWork.SaveAsync();
            
            await transaction.CommitAsync();

            return new AccountHolderResponse
            {
                Id = newAccountHolder.Id,
                FullName = $"{newAccountHolder.FirstName} {newAccountHolder.LastName}",
                NRIC = newAccountHolder.NRIC,
                Age = DateTime.Now.Year - newAccountHolder.DateOfBirth.Year,
                Balance = newEducationAccount.Balance,
                EducationLevel = newAccountHolder.EducationLevel.ToString(),
                CreatedDate = DateOnly.FromDateTime(newAccountHolder.CreatedAt).ToString("dd/MM/yyyy"),
                CreateTime = newAccountHolder.CreatedAt.ToString("HH:mm tt"),
                ResidentialStatus = newAccountHolder.ResidentialStatus,
                CourseCount = 0,
            };
        }
        catch (ValidationException)
        {
            await transaction.RollbackAsync();
            throw;
        }
        catch (DbUpdateException dbEx)
        {
            await transaction.RollbackAsync();
            
            // Check if it's a duplicate key violation
            if (dbEx.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true ||
                dbEx.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true ||
                dbEx.InnerException?.Message.Contains("NRIC", StringComparison.OrdinalIgnoreCase) == true)
            {
                throw new ValidationException("ACCOUNT_HOLDER_EXISTS", $"Account holder with NRIC {request.NRIC} already exists.");
            }
            
            throw;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            await transaction.DisposeAsync();
        }
    }

    public async Task<BulkAccountOperationResponse> ActivateAccountsAsync(BulkAccountOperationRequest request)
    {
        var response = new BulkAccountOperationResponse
        {
            TotalProcessed = request.AccountIds.Count
        };

        var educationAccountRepo = _unitOfWork.GetRepository<EducationAccount>();

        foreach (var accountId in request.AccountIds)
        {
            try
            {
                var educationAccount = await educationAccountRepo.GetByIdAsync(accountId);

                if (educationAccount == null)
                {
                    response.FailedOperations.Add(new FailedAccountOperation
                    {
                        AccountId = accountId,
                        Reason = "Education account not found"
                    });
                    response.FailedCount++;
                    continue;
                }

                if (educationAccount.IsActive)
                {
                    response.FailedOperations.Add(new FailedAccountOperation
                    {
                        AccountId = accountId,
                        Reason = "Account is already active"
                    });
                    response.FailedCount++;
                    continue;
                }

                educationAccount.IsActive = true;
                educationAccount.UpdatedAt = DateTime.UtcNow;
                
                await educationAccountRepo.UpdateAsync(educationAccount);
                response.SuccessfulIds.Add(accountId);
                response.SuccessCount++;
            }
            catch (Exception ex)
            {
                response.FailedOperations.Add(new FailedAccountOperation
                {
                    AccountId = accountId,
                    Reason = $"Error: {ex.Message}"
                });
                response.FailedCount++;
            }
        }

        await _unitOfWork.SaveAsync();
        return response;
    }

    public async Task<BulkAccountOperationResponse> DeactivateAccountsAsync(BulkAccountOperationRequest request)
    {
        var response = new BulkAccountOperationResponse
        {
            TotalProcessed = request.AccountIds.Count
        };

        var educationAccountRepo = _unitOfWork.GetRepository<EducationAccount>();

        foreach (var accountId in request.AccountIds)
        {
            try
            {
                var educationAccount = await educationAccountRepo.GetByIdAsync(accountId);

                if (educationAccount == null)
                {
                    response.FailedOperations.Add(new FailedAccountOperation
                    {
                        AccountId = accountId,
                        Reason = "Education account not found"
                    });
                    response.FailedCount++;
                    continue;
                }

                if (!educationAccount.IsActive)
                {
                    response.FailedOperations.Add(new FailedAccountOperation
                    {
                        AccountId = accountId,
                        Reason = "Account is already inactive"
                    });
                    response.FailedCount++;
                    continue;
                }

                educationAccount.IsActive = false;
                educationAccount.ClosedDate = DateTime.UtcNow;
                educationAccount.UpdatedAt = DateTime.UtcNow;
                
                await educationAccountRepo.UpdateAsync(educationAccount);
                response.SuccessfulIds.Add(accountId);
                response.SuccessCount++;
            }
            catch (Exception ex)
            {
                response.FailedOperations.Add(new FailedAccountOperation
                {
                    AccountId = accountId,
                    Reason = $"Error: {ex.Message}"
                });
                response.FailedCount++;
            }
        }

        await _unitOfWork.SaveAsync();
        return response;
    }

    public async Task UpdateAccountHolderAsync(EditAccountHolderRequest request)
    {
        var accountHolderRepo = _unitOfWork.GetRepository<AccountHolder>();

        var existingAccountHolder = accountHolderRepo.Entities
            .FirstOrDefault(ah => ah.Id == request.AccountHolderId);

        if (existingAccountHolder == null)
        {
            throw new NotFoundException("ACCOUNT_HOLDER_NOT_FOUND", $"Account holder with ID {request.AccountHolderId} not found.");
        }

        existingAccountHolder.Email = request.Email;
        existingAccountHolder.ContactNumber = request.PhoneNumber;
        existingAccountHolder.RegisteredAddress = request.RegisteredAddress;
        existingAccountHolder.MailingAddress = request.MailingAddress;
        existingAccountHolder.UpdatedAt = DateTime.UtcNow;

        await accountHolderRepo.UpdateAsync(existingAccountHolder);
        await _unitOfWork.SaveAsync();
    }

    public async Task ActivateAccountAsync(string accountHolderId)
    {
        var accountHolderRepo = _unitOfWork.GetRepository<AccountHolder>();
        
        var accountHolder = await accountHolderRepo.Entities
            .Include(ah => ah.EducationAccount)
            .FirstOrDefaultAsync(ah => ah.Id == accountHolderId);

        if (accountHolder == null)
        {
            throw new NotFoundException("ACCOUNT_HOLDER_NOT_FOUND", $"Account holder with ID {accountHolderId} not found.");
        }

        if (accountHolder.EducationAccount == null)
        {
            throw new NotFoundException("EDUCATION_ACCOUNT_NOT_FOUND", $"Education account not found for account holder {accountHolderId}.");
        }

        if (accountHolder.EducationAccount.IsActive == true)
        {
            throw new ValidationException("ACCOUNT_ALREADY_ACTIVE", "Account is already active.");
        }

        accountHolder.EducationAccount.IsActive = true;
        accountHolder.EducationAccount.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveAsync();
    }

    public async Task DeactivateAccountAsync(string accountHolderId)
    {
        var accountHolderRepo = _unitOfWork.GetRepository<AccountHolder>();
        
        var accountHolder = await accountHolderRepo.Entities
            .Include(ah => ah.EducationAccount)
            .FirstOrDefaultAsync(ah => ah.Id == accountHolderId);

        if (accountHolder == null)
        {
            throw new NotFoundException("ACCOUNT_HOLDER_NOT_FOUND", $"Account holder with ID {accountHolderId} not found.");
        }

        if (accountHolder.EducationAccount == null)
        {
            throw new NotFoundException("EDUCATION_ACCOUNT_NOT_FOUND", $"Education account not found for account holder {accountHolderId}.");
        }

        if (accountHolder.EducationAccount.IsActive == false)
        {
            throw new ValidationException("ACCOUNT_ALREADY_INACTIVE", "Account is already inactive.");
        }

        accountHolder.EducationAccount.IsActive = false;
        accountHolder.EducationAccount.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveAsync();
    }

    public async Task<StudentCourseDetailResponse> GetStudentCourseDetailAsync(string accountHolderId, string courseId)
    {
        var accountHolderRepo = _unitOfWork.GetRepository<AccountHolder>();
        var courseRepo = _unitOfWork.GetRepository<Course>();
        var enrollmentRepo = _unitOfWork.GetRepository<Enrollment>();
        var invoiceRepo = _unitOfWork.GetRepository<Invoice>();

        // Get account holder with education account
        var accountHolder = await accountHolderRepo.FirstOrDefaultAsync(
            predicate: ah => ah.Id == accountHolderId,
            include: query => query.Include(ah => ah.EducationAccount)
        );

        if (accountHolder == null)
        {
            throw new NotFoundException("ACCOUNT_HOLDER_NOT_FOUND", $"Account holder with ID {accountHolderId} not found.");
        }

        // Get course
        var course = await courseRepo.FirstOrDefaultAsync(
            predicate: c => c.Id == courseId,
            include: query => query.Include(c => c.Provider)
        );

        if (course == null)
        {
            throw new NotFoundException("COURSE_NOT_FOUND", $"Course with ID {courseId} not found.");
        }

        // Get enrollment
        var enrollment = await enrollmentRepo.FirstOrDefaultAsync(
            predicate: e => e.EducationAccountId == accountHolder.EducationAccount!.Id && e.CourseId == courseId
        );

        if (enrollment == null)
        {
            throw new NotFoundException("ENROLLMENT_NOT_FOUND", $"Student is not enrolled in this course.");
        }

        // Get all invoices for this enrollment with transactions
        var invoices = await invoiceRepo.Entities
            .Where(i => i.EnrollmentID == enrollment.Id)
            .Include(i => i.Transactions)
            .ToListAsync();

        // Calculate payment summary
        var totalCharged = invoices.Sum(i => i.Amount);
        var totalPaid = invoices
            .SelectMany(i => i.Transactions)
            .Sum(t => t.Amount);
        var outstanding = totalCharged - totalPaid;

        // Map Outstanding Fees (only Outstanding status)
        var outstandingFees = invoices
            .Where(i => i.Status == InvoiceStatus.Outstanding)
            .Select(invoice =>
            {
                var amountPaid = invoice.Transactions.Sum(t => t.Amount);
                var billingCycle = GetBillingCycleDisplay(invoice, course);
                
                return new OutstandingFeeItem(
                    invoice.Id,
                    billingCycle,
                    invoice.BillingDate,
                    invoice.DueDate ?? DateTime.MinValue,
                    invoice.Amount,
                    amountPaid,
                    invoice.Status.ToString()
                );
            })
            .OrderBy(f => f.DueDate)
            .ToList();

        // Map Payment History (only Paid status)
        var paymentHistory = invoices
            .Where(i => i.Status == InvoiceStatus.Paid)
            .Select(invoice =>
            {
                var latestTransaction = invoice.Transactions.OrderByDescending(t => t.TransactionAt).FirstOrDefault();
                var paidCycle = GetBillingCycleDisplay(invoice, course);
                var paymentMethod = FormatPaymentMethod(latestTransaction?.PaymentMethod);
                
                return new PaymentHistoryItem(
                    invoice.Id,
                    latestTransaction?.TransactionAt ?? DateTime.MinValue,
                    course.CourseName,
                    paidCycle,
                    invoice.Amount,
                    paymentMethod,
                    invoice.Status.ToString()
                );
            })
            .OrderByDescending(p => p.PaymentDate)
            .ToList();

        // Get total enrolled courses count for this account holder
        var totalEnrolledCourses = await enrollmentRepo.Entities
            .Where(e => e.EducationAccountId == accountHolder.EducationAccount!.Id)
            .CountAsync();

        // Map response
        return new StudentCourseDetailResponse(
            new AccountHolderInfo(
                accountHolder.Id,
                accountHolder.FullName,
                accountHolder.NRIC,
                accountHolder.Email ?? string.Empty,
                accountHolder.ContactNumber ?? string.Empty,
                totalEnrolledCourses
            ),
            new CourseInfo(
                course.Id,
                course.CourseCode,
                course.CourseName,
                course.Provider?.Name ?? string.Empty,
                course.EducationLevel?.ToString(),
                course.LearningType,
                course.Status,
                course.StartDate,
                course.EndDate,
                course.PaymentType.ToString(),
                course.BillingCycle,
                course.FeePerCycle ?? course.FeeAmount,
                course.FeeAmount,
                course.BillingDate,
                course.PaymentDue
            ),
            new EnrollmentInfo(
                enrollment.Id,
                enrollment.EnrollDate,
                enrollment.Status.ToString()
            ),
            new PaymentSummary(
                totalCharged,
                totalPaid,
                outstanding
            ),
            outstandingFees,
            paymentHistory
        );
    }

    private string GetBillingCycleDisplay(Invoice invoice, Course course)
    {
        // For one-time payment, show the month and year of billing date
        if (course.PaymentType == PaymentType.OneTime)
        {
            return invoice.BillingDate.ToString("MMM yyyy");
        }

        // For recurring payment, show the month and year of the billing period start
        if (invoice.BillingPeriodStart.HasValue)
        {
            return invoice.BillingPeriodStart.Value.ToString("MMM yyyy");
        }

        // Fallback to billing date
        return invoice.BillingDate.ToString("MMM yyyy");
    }

    private string FormatPaymentMethod(PaymentMethod? paymentMethod)
    {
        if (!paymentMethod.HasValue)
        {
            return "-";
        }

        return paymentMethod.Value switch
        {
            PaymentMethod.AccountBalance => "Account Balance",
            PaymentMethod.CreditDebitCard => "Credit/Debit Card",
            PaymentMethod.BankTransfer => "Bank Transfer",
            PaymentMethod.Combined => "Account Balance + Bank Transfer",
            _ => paymentMethod.Value.ToString()
        };
    }

    private DateTime? GetBillingDate(DateTime dueDate, int? billingDay)
    {
        if (!billingDay.HasValue) return null;
        
        var year = dueDate.Year;
        var month = dueDate.Month;
        
        return new DateTime(year, month, billingDay.Value);
    }
}
