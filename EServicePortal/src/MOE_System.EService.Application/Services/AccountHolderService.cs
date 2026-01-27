using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MOE_System.EService.Application.Common;
using MOE_System.EService.Application.Common.Interfaces;
using MOE_System.EService.Application.DTOs;
using MOE_System.EService.Application.DTOs.AccountHolder;
using MOE_System.EService.Application.DTOs.Course;
using MOE_System.EService.Application.DTOs.Dashboard;
using MOE_System.EService.Application.DTOs.EducationAccount;
using MOE_System.EService.Application.Interfaces;
using MOE_System.EService.Application.Interfaces.Services;
using MOE_System.EService.Domain.Common;
using MOE_System.EService.Domain.Entities;
using MOE_System.EService.Domain.Enums;
using static MOE_System.EService.Domain.Common.BaseException;

namespace MOE_System.EService.Application.Services
{
    public class AccountHolderService : IAccountHolderService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AccountHolderService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork; 
        }

        public async Task<AccountHolderResponse> GetAccountHolderAsync(string accountHolderId)
        {
            if (string.IsNullOrWhiteSpace(accountHolderId))
            {
                throw new BaseException.BadRequestException("ID must not be empty or null!");
            }

            var repo = _unitOfWork.GetRepository<AccountHolder>();

            var accountHolder = await repo.Entities.AsNoTracking()
                .Include(a => a.EducationAccount)
                .Where(a => a.Id == accountHolderId)
                .FirstOrDefaultAsync();

            if (accountHolder == null)
            {
                throw new BaseException.NotFoundException("This account is not found!");
            }

            var accountHolderResponse = new AccountHolderResponse
            {
                Id = accountHolderId,
                FullName = accountHolder.FirstName + " " + accountHolder.LastName,
                NRIC = accountHolder.NRIC,
                Email = accountHolder.Email,
                ContactNumber = accountHolder.ContactNumber,
                DateOfBirth = accountHolder.DateOfBirth,
                SchoolingStatus = accountHolder.SchoolingStatus.ToString(),    
                EducationLevel = accountHolder.EducationLevel.ToString(),
                RegisteredAddress = accountHolder.RegisteredAddress,
                MailingAddress = accountHolder.MailingAddress,
                EducationAccountId = accountHolder.EducationAccount?.Id ?? "",
                EducationAccountBalance = accountHolder.EducationAccount?.Balance ?? 0,
                IsActive = accountHolder.EducationAccount?.IsActive ?? false,
                CreatedAt = accountHolder.EducationAccount?.CreatedAt ?? DateTime.Now,
            };

            return accountHolderResponse;
        }

        public async Task<AccountHolderProfileResponse> GetMyProfileAsync(string accountHolderId)
        {
            var repo = _unitOfWork.GetRepository<AccountHolder>();

            var accountHolder = await repo.FindAsync(
                x => x.Id.ToLower() == accountHolderId.ToLower(),
                q => q.Include(x => x.EducationAccount)
            );

            if (accountHolder == null)
            {
                throw new BaseException.NotFoundException("Account holder not found!");
            }

            return new AccountHolderProfileResponse
            {
                FullName = $"{accountHolder.FirstName} {accountHolder.LastName}",
                NRIC = accountHolder.NRIC,
                DateOfBirth = accountHolder.DateOfBirth,
                AccountCreated = accountHolder.EducationAccount?.CreatedAt ?? accountHolder.CreatedAt,
                SchoolingStatus = accountHolder.SchoolingStatus.ToString(),
                EducationLevel = accountHolder.EducationLevel.ToString(),
                ResidentialStatus = accountHolder.ResidentialStatus,
                EmailAddress = accountHolder.Email,
                PhoneNumber = accountHolder.ContactNumber,
                RegisteredAddress = accountHolder.RegisteredAddress,
                MailingAddress = accountHolder.MailingAddress
            };
        }

        public async Task<UpdateProfileResponse> UpdateProfileAsync(string accountHolderId, UpdateProfileRequest request)
        {
            var accountHolderRepo = _unitOfWork.GetRepository<AccountHolder>();
            var accountHolder = await accountHolderRepo.FindAsync(x => x.Id.ToLower() == accountHolderId.ToLower());
            if (accountHolder == null)
            {
                throw new BaseException.NotFoundException("Account holder not found");
            }

            accountHolder.Email = !string.IsNullOrWhiteSpace(request.Email)
                          ? request.Email
                          : accountHolder.Email;

            accountHolder.ContactNumber = !string.IsNullOrWhiteSpace(request.ContactNumber)
                                          ? request.ContactNumber
                                          : accountHolder.ContactNumber;

            accountHolder.MailingAddress = !string.IsNullOrWhiteSpace(request.MailingAddress)
                                           ? request.MailingAddress
                                           : accountHolder.MailingAddress;

            accountHolder.RegisteredAddress = !string.IsNullOrWhiteSpace(request.RegisteredAddress)
                                              ? request.RegisteredAddress
                                              : accountHolder.RegisteredAddress;

            accountHolderRepo.Update(accountHolder);
            await _unitOfWork.SaveAsync();
            return new UpdateProfileResponse
            {
                AccountHolderId = accountHolder.Id,
                FullName = $"{accountHolder.FirstName} {accountHolder.LastName}",
                Email = accountHolder.Email,
                ContactNumber = accountHolder.ContactNumber,
                MailingAddress = accountHolder.MailingAddress,
                RegisteredAddress = accountHolder.RegisteredAddress
            };
        }
        public async Task SyncProfileAsync(string accountHolderId)
        {
            var accountHolderRepo = _unitOfWork.GetRepository<AccountHolder>();

            var accountHolder = await accountHolderRepo.FindAsync(x => x.Id == accountHolderId);

            if (accountHolder == null)
            {
                throw new BaseException.NotFoundException("Account holder not found");
            }

            var residentRepo = _unitOfWork.GetRepository<Resident>();

            var resident = await residentRepo.FindAsync(x => x.NRIC == accountHolder.NRIC);

            if (resident == null)
            {
                throw new BaseException.NotFoundException("Resident record not found");
            }

            accountHolder.FirstName = resident.PrincipalName.Split(' ').FirstOrDefault() ?? accountHolder.FirstName;
            accountHolder.LastName = resident.PrincipalName.Split(' ').Skip(1).FirstOrDefault() ?? accountHolder.LastName;
            accountHolder.DateOfBirth = resident.DateOfBirth.ToDateTime(TimeOnly.MinValue);
            accountHolder.ResidentialStatus = resident.ResidentialStatus;
            accountHolder.RegisteredAddress = resident.RegisteredAddress;
            accountHolder.Email = resident.EmailAddress;
            accountHolder.ContactNumber = resident.MobileNumber;

            accountHolderRepo.Update(accountHolder);

            await _unitOfWork.SaveAsync();
        }

        public async Task<CourseDetailResponse> GetCourseDetailAsync(string accountHolderId, string enrollmentId)
        {
            var enrollmentRepo = _unitOfWork.GetRepository<Enrollment>();

            var enrollment = await enrollmentRepo.Entities
                .Include(e => e.EducationAccount)
                    .ThenInclude(ea => ea!.AccountHolder)
                .Include(e => e.Course)
                    .ThenInclude(c => c!.Provider)
                .Include(e => e.Invoices)
                    .ThenInclude(i => i.Transactions)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId);

            if (enrollment == null)
            {
                throw new NotFoundException("Enrollment not found!");
            }

            if (enrollment.EducationAccount?.AccountHolder?.Id != accountHolderId)
            {
                throw new BaseException.UnauthorizedException("You do not have access to this course!");
            }

            var course = enrollment.Course;
            if (course == null)
            {
                throw new NotFoundException("Course not found!");
            }

            var invoices = enrollment.Invoices ?? new List<Invoice>();
            var totalCharged = invoices.Sum(i => i.Amount);
            var totalPaid = invoices
                .SelectMany(i => i.Transactions ?? new List<Transaction>())
                .Where(t => t.Status == TransactionStatus.Success)
                .Sum(t => t.Amount);
            var outstanding = totalCharged - totalPaid;

            var paymentHistory = invoices
                .Where(i => i.Status == InvoiceStatus.Paid)
                .SelectMany(invoice =>
                {
                    var successfulTransactions = invoice.Transactions?
                        .Where(t => t.Status == TransactionStatus.Success)
                        .OrderByDescending(t => t.TransactionAt)
                        .ToList() ?? new List<Transaction>();

                    return successfulTransactions.Select(transaction =>
                    {
                        var paidCycle = GetBillingCycleDisplay(invoice);
                        var paymentMethod = FormatPaymentMethod(transaction.PaymentMethod);

                        return new PaymentHistoryDetail
                        {
                            InvoiceId = invoice.Id,
                            PaymentDate = transaction.TransactionAt ?? DateTime.MinValue,
                            CourseName = course.CourseName,
                            PaidCycle = paidCycle,
                            Amount = transaction.Amount,
                            PaymentMethod = paymentMethod,
                            Status = invoice.Status.ToString()
                        };
                    });
                })
                .OrderByDescending(p => p.PaymentDate)
                .ToList();

            return new CourseDetailResponse
            {
                Course = new CourseInformation
                {
                    CourseId = course.Id,
                    CourseName = course.CourseName,
                    ProviderName = course.Provider?.Name ?? "Unknown",
                    CourseStart = course.StartDate,
                    CourseEnd = course.EndDate,
                    PaymentType = course.PaymentType.ToString(),
                    BillingCycle = course.BillingCycle,
                    Status = course.Status,
                    TotalFee = course.FeeAmount
                },
                PaymentSummary = new PaymentSummaryInfo
                {
                    TotalCharged = totalCharged,
                    TotalPaid = totalPaid,
                    Outstanding = outstanding
                },
                PaymentHistory = paymentHistory
            };
        }

        private string GetBillingCycleDisplay(Invoice invoice)
        {
            // Return billing period as "Month Year" format
            if (invoice.BillingDate != default)
            {
                return invoice.BillingDate.ToString("MMMM yyyy");
            }

            return "One Time";
        }

        private string FormatPaymentMethod(PaymentMethod paymentMethod)
        {
            return paymentMethod switch
            {
                PaymentMethod.AccountBalance => "Account Balance",
                PaymentMethod.CreditDebitCard => "Credit Card",
                PaymentMethod.BankTransfer => "Bank Transfer",
                _ => paymentMethod.ToString()
            };
        }

        public async Task<CourseSummaryResponse> GetCourseSummaryAsync(string accountHolderId)
        {
            var accountHolderRepo = _unitOfWork.GetRepository<AccountHolder>();

            var accountHolder = await accountHolderRepo.Entities
                .Where(ah => ah.Id == accountHolderId)
                .Include(ah => ah.EducationAccount)
                    .ThenInclude(ea => ea!.Enrollments)
                        .ThenInclude(e => e.Invoices)
                .FirstOrDefaultAsync();

            if (accountHolder?.EducationAccount == null)
            {
                return new CourseSummaryResponse();
            }

            var eduAccount = accountHolder.EducationAccount;
            var enrollments = eduAccount.Enrollments ?? new List<Enrollment>();

            var allInvoices = enrollments
                .SelectMany(e => e.Invoices ?? new List<Invoice>())
                .ToList();

            var outstandingFees = allInvoices
                .Where(i => i.Status == InvoiceStatus.Outstanding)
                .Sum(i => i.Amount);

            var totalPendingInvoices = allInvoices
                .Count(i => i.Status == InvoiceStatus.Outstanding);

            return new CourseSummaryResponse
            {
                OutstandingFees = outstandingFees,
                Balance = eduAccount.Balance,
                TotalEnrolledCourses = enrollments.Count,
                TotalPendingInvoices = totalPendingInvoices
            };
        }

        public async Task<EnrolledCoursesResponse> GetEnrolledCoursesAsync(string accountHolderId, EnrolledCoursesRequest request)
        {
            var enrollmentRepo = _unitOfWork.GetRepository<Enrollment>();
            var accountHolderRepo = _unitOfWork.GetRepository<AccountHolder>();

            var accountHolder = await accountHolderRepo.Entities
                .Where(ah => ah.Id == accountHolderId)
                .Include(ah => ah.EducationAccount)
                .FirstOrDefaultAsync();

            if (accountHolder?.EducationAccount == null)
            {
                return new EnrolledCoursesResponse
                {
                    Courses = new PaginatedList<EnrolledCourse>(new List<EnrolledCourse>(), 0, request.PageNumber, request.PageSize)
                };
            }

            var enrollments = await enrollmentRepo.Entities
                .Where(e => e.EducationAccountId == accountHolder.EducationAccount.Id)
                .Include(e => e.Course!)
                    .ThenInclude(c => c.Provider)
                .Include(e => e.Invoices)
                .OrderByDescending(e => e.EnrollDate)
                .ToListAsync();

            var enrolledCourses = enrollments.Select(e => new EnrolledCourse
            {
                EnrollmentId = e.Id,
                CourseName = e.Course!.CourseName,
                ProviderName = e.Course.Provider!.Name,
                CourseFee = e.Course.FeeAmount,
                BillingCycle = e.Course.BillingCycle ?? string.Empty,
                EnrolledDate = e.EnrollDate.ToString("dd/MM/yyyy"),
                BillingDate = GetBillingDateForEnrollment(e),
                PaymentStatus = GetPaymentStatusForEnrollment(e)
            }).AsQueryable();

            var paginatedCourses = PaginatedList<EnrolledCourse>.Create(
                enrolledCourses,
                request.PageNumber,
                request.PageSize
            );

            return new EnrolledCoursesResponse
            {
                Courses = paginatedCourses
            };
        }

        public async Task<PendingFeesResponse> GetPendingFeesAsync(string accountHolderId, PendingFeesRequest request)
        {
            var accountHolderRepo = _unitOfWork.GetRepository<AccountHolder>();
            var invoiceRepo = _unitOfWork.GetRepository<Invoice>();

            var accountHolder = await accountHolderRepo.Entities
                .Where(ah => ah.Id == accountHolderId)
                .Include(ah => ah.EducationAccount)
                .FirstOrDefaultAsync();

            if (accountHolder?.EducationAccount == null)
            {
                return new PendingFeesResponse
                {
                    Fees = new PaginatedList<PendingFees>(new List<PendingFees>(), 0, request.PageNumber, request.PageSize)
                };
            }

            var query = invoiceRepo.Entities
                .Where(i => i.Enrollment!.EducationAccountId == accountHolder.EducationAccount.Id 
                    && i.Status == InvoiceStatus.Outstanding)
                .Include(i => i.Enrollment!)
                    .ThenInclude(e => e.Course!)
                        .ThenInclude(c => c.Provider)   
                .OrderBy(i => i.DueDate)
                .Select(i => new PendingFees
                {
                    InvoiceId = i.Id,
                    CourseName = i.Enrollment!.Course!.CourseName,
                    ProviderName = i.Enrollment.Course.Provider!.Name,
                    AmountDue = i.Amount,
                    BillingCycle = i.Enrollment.Course.BillingCycle ?? "-",
                    BillingDate = i.BillingDate.ToString("dd/MM/yyyy"),
                    DueDate = i.DueDate.HasValue ? i.DueDate.Value.ToString("dd/MM/yyyy") : "-",
                    PaymentStatus = i.Status.ToString()
                });

            var paginatedFees = await PaginatedList<PendingFees>.CreateAsync(
                query,
                request.PageNumber,
                request.PageSize
            );

            return new PendingFeesResponse
            {
                Fees = paginatedFees
            };
        }

        public async Task<PaymentHistoryResponse> GetPaymentHistoryAsync(string accountHolderId, PaymentHistoryRequest request)
        {
            var accountHolderRepo = _unitOfWork.GetRepository<AccountHolder>();
            var invoiceRepo = _unitOfWork.GetRepository<Invoice>();

            var accountHolder = await accountHolderRepo.Entities
                .Where(ah => ah.Id == accountHolderId)
                .Include(ah => ah.EducationAccount)
                .FirstOrDefaultAsync();

            if (accountHolder?.EducationAccount == null)
            {
                return new PaymentHistoryResponse
                {
                    History = new PaginatedList<PaymentHistory>(new List<PaymentHistory>(), 0, request.PageNumber, request.PageSize)
                };
            }

            var rawTransactions = await invoiceRepo.Entities
                .Where(i => i.Enrollment!.EducationAccountId == accountHolder.EducationAccount.Id
                    && i.Status == InvoiceStatus.Paid)
                .Include(i => i.Enrollment!)
                    .ThenInclude(e => e.Course!)
                        .ThenInclude(c => c.Provider)
                .SelectMany(i => i.Transactions!
                    .Where(t => t.Status == TransactionStatus.Success)
                    .Select(t => new
                    {
                        InvoiceId = i.Id,
                        CourseName = i.Enrollment!.Course!.CourseName,
                        ProviderName = i.Enrollment.Course.Provider!.Name,
                        BillingCycle = i.Enrollment.Course.BillingCycle,
                        Transaction = t
                    }))
                .ToListAsync();

            var groupedList = rawTransactions
                .GroupBy(x => x.InvoiceId)
                .Select(g => new
                {
                    LatestDate = g.Max(x => x.Transaction.TransactionAt),
                    Item = new PaymentHistory
                    {
                        InvoiceId = g.Key,
                        CourseName = g.First().CourseName,
                        ProviderName = g.First().ProviderName,
                        BillingCycle = g.First().BillingCycle ?? "-",
                        AmountPaid = g.Sum(x => x.Transaction.Amount),
                        Transactions = g.OrderByDescending(x => x.Transaction.TransactionAt)
                            .Select(x => new PaymentTransaction
                            {
                                Amount = x.Transaction.Amount,
                                TransactionDate = x.Transaction.TransactionAt,
                                PaymentMethodRaw = (int?)x.Transaction.PaymentMethod,
                                PaymentMethod = FormatPaymentMethod(x.Transaction.PaymentMethod)
                            })
                            .ToList(),
                        PaymentMethod = string.Join(" / ", g
                            .Select(x => FormatPaymentMethod(x.Transaction.PaymentMethod))
                            .Where(s => !string.IsNullOrEmpty(s))
                            .Distinct())
                    }
                })
                .OrderByDescending(x => x.LatestDate)
                .Select(x => x.Item)
                .AsQueryable();

            var paginatedHistory = PaginatedList<PaymentHistory>.Create(
                groupedList,
                request.PageNumber,
                request.PageSize
            );

            foreach (var item in paginatedHistory.Items)
            {
                if (string.IsNullOrWhiteSpace(item.PaymentMethod))
                {
                    item.PaymentMethod = "-";
                }
            }

            return new PaymentHistoryResponse
            {
                History = paginatedHistory
            };
        }

        #region Course Details by Enrollment

        public async Task<CourseInformationResponse> GetCourseInformationAsync(string accountHolderId, string enrollmentId)
        {
            var enrollmentRepo = _unitOfWork.GetRepository<Enrollment>();

            var enrollment = await enrollmentRepo.Entities
                .Include(e => e.EducationAccount)
                    .ThenInclude(ea => ea!.AccountHolder)
                .Include(e => e.Course!)
                    .ThenInclude(c => c.Provider)
                .Include(e => e.Invoices)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId);

            if (enrollment == null)
            {
                throw new NotFoundException("Enrollment not found!");
            }

            if (enrollment.EducationAccount?.AccountHolder?.Id != accountHolderId)
            {
                throw new UnauthorizedException("You do not have access to this course!");
            }

            var course = enrollment.Course!;
            var invoices = enrollment.Invoices ?? new List<Invoice>();

            var totalOutstanding = invoices
                .Where(i => i.Status == InvoiceStatus.Outstanding)
                .Sum(i => i.Amount);

            return new CourseInformationResponse
            {
                CourseName = course.CourseName,
                ProviderName = course.Provider?.Name ?? "Unknown",
                CourseStart = course.StartDate.ToString("dd/MM/yyyy"),
                CourseEnd = course.EndDate.ToString("dd/MM/yyyy"),
                PaymentType = course.PaymentType.ToString(),
                BillingCycle = course.BillingCycle ?? "-",
                Status = course.Status,
                EnrolledDate = enrollment.EnrollDate.ToString("dd/MM/yyyy"),
                EducationLevel = course.EducationLevel != null ? course.EducationLevel.Value.ToFriendlyString() : "-",
                FeePerCycle = course.FeePerCycle ?? course.FeeAmount,
                TotalOutstandingFee = totalOutstanding,
                CourseTotalFee = course.FeeAmount
            };
        }

        public async Task<OutstandingFeesResponse> GetOutstandingFeesAsync(string accountHolderId, string enrollmentId, OutstandingFeesRequest request)
        {
            var enrollmentRepo = _unitOfWork.GetRepository<Enrollment>();
            var invoiceRepo = _unitOfWork.GetRepository<Invoice>();

            var enrollment = await enrollmentRepo.Entities
                .Include(e => e.EducationAccount)
                    .ThenInclude(ea => ea!.AccountHolder)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId);

            if (enrollment == null)
            {
                throw new NotFoundException("Enrollment not found!");
            }

            if (enrollment.EducationAccount?.AccountHolder?.Id != accountHolderId)
            {
                throw new UnauthorizedException("You do not have access to this course!");
            }

            var today = DateTime.Today;

            var query = invoiceRepo.Entities
                .Where(i => i.EnrollmentID == enrollmentId && i.Status == InvoiceStatus.Outstanding)
                .Include(i => i.Enrollment!)
                    .ThenInclude(e => e.Course!)
                        .ThenInclude(c => c.Provider)
                .OrderBy(i => i.DueDate)
                .Select(i => new OutstandingFeeItem
                {
                    InvoiceId = i.Id,
                    CourseName = i.Enrollment!.Course!.CourseName,
                    ProviderName = i.Enrollment.Course.Provider!.Name,
                    Amount = i.Amount,
                    BillingCycle = i.Enrollment.Course.BillingCycle ?? "-",
                    BillingDate = i.BillingDate.ToString("dd/MM/yyyy"),
                    DueDate = i.DueDate.HasValue ? i.DueDate.Value.ToString("dd/MM/yyyy") : "-",
                    DaysUntilDue = i.DueDate.HasValue ? (i.DueDate.Value.Date - today).Days : 0,
                    PaymentStatus = i.Status.ToString()
                });

            var paginatedFees = await PaginatedList<OutstandingFeeItem>.CreateAsync(
                query,
                request.PageNumber,
                request.PageSize
            );

            return new OutstandingFeesResponse
            {
                Fees = paginatedFees
            };
        }

        public async Task<UpcomingBillingCyclesResponse> GetUpcomingBillingCyclesAsync(
            string accountHolderId,
            string enrollmentId,
            UpcomingBillingCyclesRequest request)
        {
            var enrollmentRepo = _unitOfWork.GetRepository<Enrollment>();
            var enrollment = await enrollmentRepo.Entities
                .Include(e => e.EducationAccount)
                    .ThenInclude(ea => ea!.AccountHolder)
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId);

            if (enrollment == null)
            {
                throw new NotFoundException("Enrollment not found!");
            }

            if (enrollment.EducationAccount?.AccountHolder?.Id != accountHolderId)
            {
                throw new UnauthorizedException("You do not have access to this course!");
            }

            if (enrollment.Course == null)
            {
                throw new NotFoundException("Course not found!");
            }

            if (enrollment.Course.PaymentType != PaymentType.Recurring)
            {
                return new UpcomingBillingCyclesResponse
                {
                    BillingCycles = new PaginatedList<UpcomingBillingCycleItem>(
                        new List<UpcomingBillingCycleItem>(),
                        0,
                        request.PageNumber,
                        request.PageSize)
                };
            }

            var today = DateTime.Today;
            var course = enrollment.Course;

            var billingPeriods = BillingPeriodResolver.Resolve(
                course.StartDate,
                course.BillingCycle!,
                course.EndDate
            );

            var billingDate = course.BillingDate ?? 1;
            var paymentDue = course.PaymentDue ?? 15;
            var feePerCycle = course.FeePerCycle ?? 0;
            var enrollmentDate = enrollment.EnrollDate;

            var billingCycles = new List<UpcomingBillingCycleItem>();

            foreach (var period in billingPeriods)
            {
                if (enrollmentDate >= period.End)
                {
                    continue;
                }

                DateTime invoiceDate;

                if (enrollmentDate >= period.Start && enrollmentDate < period.End)
                {
                    invoiceDate = enrollmentDate;
                }
                else
                {
                    invoiceDate = new DateTime(
                        period.Start.Year,
                        period.Start.Month,
                        Math.Min(billingDate, DateTime.DaysInMonth(period.Start.Year, period.Start.Month))
                    );
                }

                var dueDate = invoiceDate.AddDays(paymentDue);

                if (dueDate >= today)
                {
                    billingCycles.Add(new UpcomingBillingCycleItem
                    {
                        InvoiceId = string.Empty,
                        DueMonth = period.Start.ToString("MMMM yyyy"),
                        BillingDate = invoiceDate.ToString("dd/MM/yyyy"),
                        DueDate = dueDate.ToString("dd/MM/yyyy"),
                        Amount = feePerCycle,
                        Status = PaymentStatus.Scheduled.ToString(),
                    });
                }
            }

            billingCycles = billingCycles
                .OrderBy(bc => DateTime.ParseExact(bc.DueDate, "dd/MM/yyyy", null))
                .ToList();

            var totalCount = billingCycles.Count;
            var pagedCycles = billingCycles
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var paginatedResult = new PaginatedList<UpcomingBillingCycleItem>(
                pagedCycles,
                totalCount,
                request.PageNumber,
                request.PageSize
            );

            return new UpcomingBillingCyclesResponse
            {
                BillingCycles = paginatedResult
            };
        }
       
        public async Task<EnrollmentPaymentHistoryResponse> GetEnrollmentPaymentHistoryAsync(string accountHolderId, string enrollmentId, EnrollmentPaymentHistoryRequest request)
        {
            var enrollmentRepo = _unitOfWork.GetRepository<Enrollment>();
            var invoiceRepo = _unitOfWork.GetRepository<Invoice>();

            var enrollment = await enrollmentRepo.Entities
                .Include(e => e.EducationAccount)
                    .ThenInclude(ea => ea!.AccountHolder)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId);

            if (enrollment == null)
            {
                throw new NotFoundException("Enrollment not found!");
            }

            if (enrollment.EducationAccount?.AccountHolder?.Id != accountHolderId)
            {
                throw new UnauthorizedException("You do not have access to this course!");
            }

            var rawTransactions = await invoiceRepo.Entities
                .Where(i => i.EnrollmentID == enrollmentId && i.Status == InvoiceStatus.Paid)
                .Include(i => i.Enrollment!)
                    .ThenInclude(e => e.Course!)
                        .ThenInclude(c => c.Provider)
                .SelectMany(i => i.Transactions!
                    .Where(t => t.Status == TransactionStatus.Success)
                    .Select(t => new
                    {
                        InvoiceId = i.Id,
                        InvoiceBillingDate = i.BillingDate,
                        InvoiceStatus = i.Status,
                        CourseName = i.Enrollment!.Course!.CourseName,
                        ProviderName = i.Enrollment.Course.Provider!.Name,
                        BillingCycle = i.Enrollment.Course.BillingCycle,
                        Transaction = t
                    }))
                .ToListAsync();

            var groupedList = rawTransactions
                .GroupBy(x => x.InvoiceId)
                .Select(g => new
                {
                    LatestDate = g.Max(x => x.Transaction.TransactionAt),
                    Item = new EnrollmentPaymentHistoryItem
                    {
                        InvoiceId = g.Key,
                        CourseName = g.First().CourseName,
                        ProviderName = g.First().ProviderName,
                        BillingCycle = g.First().BillingCycle ?? "-",
                        PaidCycle = g.First().InvoiceBillingDate != default
                            ? g.First().InvoiceBillingDate.ToString("MMMM yyyy")
                            : (g.Max(x => x.Transaction.TransactionAt)?.ToString("MMMM yyyy") ?? "-"),
                        Amount = g.Sum(x => x.Transaction.Amount),
                        Transactions = g.OrderByDescending(x => x.Transaction.TransactionAt)
                            .Select(x => new PaymentTransaction
                            {
                                Amount = x.Transaction.Amount,
                                TransactionDate = x.Transaction.TransactionAt,
                                PaymentMethodRaw = (int?)x.Transaction.PaymentMethod,
                                PaymentMethod = FormatPaymentMethod(x.Transaction.PaymentMethod)
                            })
                            .ToList(),
                        PaymentMethod = string.Join(" / ", g
                            .Select(x => FormatPaymentMethod(x.Transaction.PaymentMethod))
                            .Where(s => !string.IsNullOrEmpty(s))
                            .Distinct()),
                    }
                })
                .OrderByDescending(x => x.LatestDate)
                .Select(x => x.Item)
                .AsQueryable();

            var paginatedHistory = PaginatedList<EnrollmentPaymentHistoryItem>.Create(
                groupedList,
                request.PageNumber,
                request.PageSize
            );

            foreach (var item in paginatedHistory.Items)
            {
                if (string.IsNullOrWhiteSpace(item.PaymentMethod))
                {
                    item.PaymentMethod = "-";
                }
            }

            return new EnrollmentPaymentHistoryResponse
            {
                PaymentHistory = paginatedHistory
            };
        }

        #endregion

        #region Account Balance Transaction History
        public async Task<BalanceResponse> GetBalanceAsync(string accountHolderId)
        {
            var accountHolderRepo = _unitOfWork.GetRepository<AccountHolder>();
            var accountHolder = await accountHolderRepo.Entities
                .Include(ah => ah.EducationAccount)
                .FirstOrDefaultAsync(ah => ah.Id == accountHolderId);

            if (accountHolder?.EducationAccount == null) 
            {
                return new BalanceResponse { Balance = 0 };
            }

            return new BalanceResponse { Balance = accountHolder.EducationAccount.Balance };
        }

        public async Task<BalanceHistoryResponse> GetTransactionHistoryAsync(string accountHolderId, BalanceHistoryRequest request)
        {
            var accountHolderRepo = _unitOfWork.GetRepository<AccountHolder>();
            var historyRepo = _unitOfWork.GetRepository<HistoryOfChange>();

            var accountHolder = await accountHolderRepo.Entities
                .Include(ah => ah.EducationAccount)
                .FirstOrDefaultAsync(ah => ah.Id == accountHolderId);

            if (accountHolder?.EducationAccount == null)
            {
                return new BalanceHistoryResponse
                {
                    History = new PaginatedList<TransactionHistoryItem>(
                        new List<TransactionHistoryItem>(), 0, request.PageNumber, request.PageSize)
                };
            }

            var query = historyRepo.Entities
                .Where(h => h.EducationAccountId == accountHolder.EducationAccount.Id);

            if(request.SearchTerm != null)
            {
                var searchTerm = request.SearchTerm.Trim().ToLower();
                query = query.Where(h =>
                    (h.Description != null && h.Description.ToLower().Contains(searchTerm))
                );
            }
            // Apply type filter
            if (request.Type.HasValue)
            {
                query = query.Where(h => h.Type == request.Type.Value);
            }

            // Apply date filter
            query = ApplyDateFilter(query, request);

            // Apply amount filter
            query = ApplyAmountFilter(query, request);

            query = query.OrderByDescending(h => h.CreatedAt);

            var historyItemsQuery = query
                .Select(h => new TransactionHistoryItem
                {
                    Id = h.Id,
                    Type = h.Type.ToString(),
                    ReferenceId = h.ReferenceId,
                    Description = h.Description,
                    Amount = h.Amount,
                    BalanceAfter = h.BalanceAfter,
                    TransactionDate = h.CreatedAt.ToString("dd/MM/yyyy"),
                });

            var pagedHistory = await PaginatedList<TransactionHistoryItem>.CreateAsync(
                historyItemsQuery,
                request.PageNumber,
                request.PageSize
            );

            return new BalanceHistoryResponse
            {
                History = pagedHistory
            };
        }

        private IQueryable<HistoryOfChange> ApplyDateFilter(IQueryable<HistoryOfChange> query, BalanceHistoryRequest request)
        {
            var today = DateTime.Today;

            if (request.DateFilter.HasValue)
            {
                switch (request.DateFilter.Value)
                {
                    case DateFilterType.Today:
                        query = query.Where(h => h.CreatedAt.Date == today);
                        break;
                    case DateFilterType.Last7Days:
                        var last7Days = today.AddDays(-7);
                        query = query.Where(h => h.CreatedAt >= last7Days);
                        break;
                    case DateFilterType.Last30Days:
                        var last30Days = today.AddDays(-30);
                        query = query.Where(h => h.CreatedAt >= last30Days);
                        break;
                    case DateFilterType.Last3Months:
                        var last3Months = today.AddMonths(-3);
                        query = query.Where(h => h.CreatedAt >= last3Months);
                        break;
                    case DateFilterType.LastYear:
                        var lastYear = today.AddYears(-1);
                        query = query.Where(h => h.CreatedAt >= lastYear);
                        break;
                    case DateFilterType.CustomRange:
                        if (request.DateFrom.HasValue)
                        {
                            query = query.Where(h => h.CreatedAt >= request.DateFrom.Value);
                        }
                        if (request.DateTo.HasValue)
                        {
                            var dateTo = request.DateTo.Value.Date.AddDays(1).AddTicks(-1);
                            query = query.Where(h => h.CreatedAt <= dateTo);
                        }
                        break;
                }
            }

            return query;
        }

        private IQueryable<HistoryOfChange> ApplyAmountFilter(IQueryable<HistoryOfChange> query, BalanceHistoryRequest request)
        {
            if (request.AmountRange.HasValue)
            {
                switch (request.AmountRange.Value)
                {
                    case AmountRangeType.Range0To50:
                        query = query.Where(h => h.Amount >= 0 && h.Amount <= 50);
                        break;
                    case AmountRangeType.Range50To100:
                        query = query.Where(h => h.Amount > 50 && h.Amount <= 100);
                        break;
                    case AmountRangeType.Range100To500:
                        query = query.Where(h => h.Amount > 100 && h.Amount <= 500);
                        break;
                    case AmountRangeType.Range500To1000:
                        query = query.Where(h => h.Amount > 500 && h.Amount <= 1000);
                        break;
                    case AmountRangeType.Range1000Plus:
                        query = query.Where(h => h.Amount > 1000);
                        break;
                    case AmountRangeType.CustomRange:
                        if (request.AmountFrom.HasValue)
                        {
                            query = query.Where(h => h.Amount >= request.AmountFrom.Value);
                        }
                        if (request.AmountTo.HasValue)
                        {
                            query = query.Where(h => h.Amount <= request.AmountTo.Value);
                        }
                        break;
                }
            }

            return query;
        }

        private string GetBillingDateForEnrollment(Enrollment enrollment)
        {
            var paymentStatus = enrollment.Status;
            var course = enrollment.Course;
            
            if (course == null || !course.BillingDate.HasValue)
            {
                return "-";
            }

            // Outstanding or Fully Paid: show "-"
            if (paymentStatus == PaymentStatus.FullyPaid)
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
                return CalculateNextBillingDateFromDate(lastPaidInvoice.BillingDate, course.BillingDate.Value, course.BillingCycle);
            }
            
            // No paid invoices yet - calculate first billing date
            // If course start date <= billing day in start month: bill on billing day
            // If course start date > billing day in start month: bill on start date
            var enrollmentDate = enrollment.EnrollDate;
            var billingDay = course.BillingDate.Value;
            
            if (enrollmentDate.Day <= billingDay)
            {
                // Bill on the billing day of the start month
                var daysInMonth = DateTime.DaysInMonth(enrollmentDate.Year, enrollmentDate.Month);
                var actualDay = Math.Min(billingDay, daysInMonth);
                var firstBillingDate = new DateTime(enrollmentDate.Year, enrollmentDate.Month, actualDay);
                return firstBillingDate.ToString("dd/MM/yyyy");
            }
            else
            {
                // Bill on course start date (since it's after billing day)
                return enrollmentDate.ToString("dd/MM/yyyy");
            }
        }

        private string CalculateNextBillingDateFromDate(DateTime lastBillingDate, int billingDayOfMonth, string? billingCycle)
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

        private string GetPaymentStatusForEnrollment(Enrollment enrollment)
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

        #endregion
    }
}
