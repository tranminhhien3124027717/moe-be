using System.Linq.Expressions;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using MOE_System.Application.Common;
using MOE_System.Application.Common.Course;
using MOE_System.Application.Common.Interfaces;
using MOE_System.Application.DTOs.Course.Request;
using MOE_System.Application.DTOs.Course.Response;
using MOE_System.Application.Interfaces.Services;
using MOE_System.Domain.Common;
using MOE_System.Application.Interfaces;
using MOE_System.Domain.Entities;
using MOE_System.Domain.Enums;
using static MOE_System.Domain.Common.BaseException;

namespace MOE_System.Application.Services
{
    public class CourseService : ICourseService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CourseService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PaginatedList<CourseListResponse>> GetCoursesAsync(GetCourseRequest request, CancellationToken cancellationToken = default)
        {
            var courseRepo = _unitOfWork.GetRepository<Course>();

            var predicate = BuildFilterPredicate(request);

            IQueryable<Course> query = courseRepo.Entities.AsNoTracking()
                .Include(c => c.Provider)
                .Include(c => c.Enrollments)
                .Where(predicate.Expand());

            query = ApplySorting(query, request.SortBy, request.SortDirection);

            var pagedCourses = await courseRepo.GetPagging(query, request.PageNumber, request.PageSize);

            var responses = pagedCourses.Items.Select(c => new CourseListResponse(
                c.Id,
                c.CourseCode,
                c.CourseName,
                c.Provider != null ? c.Provider.Name : string.Empty,
                c.LearningType,
                c.StartDate,
                c.EndDate,
                c.PaymentType.ToString(),
                c.BillingCycle!,
                c.FeeAmount,
                c.Enrollments.Count,
                c.EducationLevel?.ToString(),
                c.BillingDate,
                c.PaymentDue
            )).ToList();

        return new PaginatedList<CourseListResponse>(responses, pagedCourses.TotalCount, pagedCourses.PageIndex, request.PageSize);
    }

    private static Expression<Func<Course, bool>> BuildFilterPredicate(GetCourseRequest request)
    {
        var predicate = PredicateBuilder.New<Course>(true);

            // Search by course name or course code
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                predicate = predicate.And(x => x.CourseName.Contains(request.SearchTerm) || x.CourseCode.Contains(request.SearchTerm) || x.Provider != null && x.Provider.Name.Contains(request.SearchTerm));

            if (request.Provider != null && request.Provider.Count > 0)
                predicate = predicate.And(x => x.ProviderId != null && request.Provider.Contains(x.ProviderId));

            if (request.ModeOfTraining != null && request.ModeOfTraining.Count > 0)
                predicate = predicate.And(x => request.ModeOfTraining.Contains(x.LearningType));

            if (request.Status != null && request.Status.Count > 0)
                predicate = predicate.And(x => request.Status.Contains(x.Status));

            if (request.PaymentType != null && request.PaymentType.Count > 0)
                predicate = predicate.And(x => request.PaymentType.Contains(x.PaymentType.ToString()));

            if (request.BillingCycle != null && request.BillingCycle.Count > 0)
                predicate = predicate.And(x => x.BillingCycle != null && request.BillingCycle.Contains(x.BillingCycle));

            if (request.StartDate.HasValue)
                predicate = predicate.And(x => x.StartDate >= request.StartDate.Value.ToDateTime(TimeOnly.MinValue));

            if (request.EndDate.HasValue)
                predicate = predicate.And(x => x.EndDate <= request.EndDate.Value.ToDateTime(TimeOnly.MinValue));

            if (request.TotalFeeMin.HasValue)
                predicate = predicate.And(x => x.FeeAmount >= request.TotalFeeMin.Value);

            if (request.TotalFeeMax.HasValue)
                predicate = predicate.And(x => x.FeeAmount <= request.TotalFeeMax.Value);

            return predicate;
        }

        private static IQueryable<Course> ApplySorting(IQueryable<Course> query, CourseSortField? sortBy, SortDirection? sortDirection)
        {
            return (sortBy, sortDirection) switch
            {
                (CourseSortField.CourseName, SortDirection.Asc) => query.OrderBy(c => c.CourseName),
                (CourseSortField.CourseName, SortDirection.Desc) => query.OrderByDescending(c => c.CourseName),
                (CourseSortField.Provider, SortDirection.Asc) => query.OrderBy(c => c.Provider!.Name),
                (CourseSortField.Provider, SortDirection.Desc) => query.OrderByDescending(c => c.Provider!.Name),
                (CourseSortField.TotalFee, SortDirection.Asc) => query.OrderBy(c => c.FeeAmount),
                (CourseSortField.TotalFee, SortDirection.Desc) => query.OrderByDescending(c => c.FeeAmount),
                (CourseSortField.StartDate, SortDirection.Asc) => query.OrderBy(c => c.StartDate),
                (CourseSortField.StartDate, SortDirection.Desc) => query.OrderByDescending(c => c.StartDate),
                (CourseSortField.EndDate, SortDirection.Asc) => query.OrderBy(c => c.EndDate),
                (CourseSortField.EndDate, SortDirection.Desc) => query.OrderByDescending(c => c.EndDate),
                (CourseSortField.CreatedAt, SortDirection.Asc) => query.OrderBy(c => c.CreatedAt),
                _ => query.OrderByDescending(c => c.CreatedAt),
            };
        }

        public async Task<CourseResponse> AddCourseAsync(AddCourseRequest request)
        {
            // Validate that the provider exists
            var providerRepo = _unitOfWork.GetRepository<Provider>();
            var provider = await providerRepo.Entities
                .FirstOrDefaultAsync(p => p.Id == request.ProviderId);

            if (provider == null)
            {
                throw new NotFoundException("PROVIDER_NOT_FOUND", $"Provider with ID {request.ProviderId} not found.");
            }

            // Calculate duration in months
            var durationMonths = CalculateDurationInMonths(request.CourseStartDate, request.CourseEndDate);

            // Calculate total fee and fee per cycle based on payment option
            decimal totalFee = 0;
            decimal? feePerCycle = null;

            if (request.PaymentOption == "One-time")
            {
                totalFee = request.TotalFee ?? 0;
            }
            else if (request.PaymentOption == "Recurring")
            {
                if (request.FeePerCycle.HasValue)
                {
                    feePerCycle = request.FeePerCycle.Value;
                    // Calculate total fee based on fee per cycle and billing cycle
                    totalFee = CalculateTotalFeeFromCycle(feePerCycle.Value, request.BillingCycle!, durationMonths);
                }
                else if (request.TotalFee.HasValue)
                {
                    totalFee = request.TotalFee.Value;
                    // Calculate fee per cycle based on total fee and billing cycle
                    feePerCycle = CalculateFeePerCycle(totalFee, request.BillingCycle!, durationMonths);
                }
            }
            //feePerCycle = request.FeePerCycle ?? 0;


            // Generate course code if not provided
            var courseCode = !string.IsNullOrWhiteSpace(request.CourseCode) 
                ? request.CourseCode 
                : await GenerateCourseCodeAsync(request.CourseName, totalFee, request.BillingCycle, request.PaymentOption);

            // Parse PaymentType - handle "One-time" to "OneTime" conversion
            var paymentTypeString = request.PaymentOption.Replace("-", "");
            var paymentType = Enum.Parse<Domain.Enums.PaymentType>(paymentTypeString, ignoreCase: true);

            // Parse EducationLevel if provided
            Domain.Enums.EducationLevel? educationLevel = null;
            if (!string.IsNullOrWhiteSpace(request.EducationLevel))
            {
                if (Enum.TryParse<Domain.Enums.EducationLevel>(request.EducationLevel, ignoreCase: true, out var parsedLevel))
                {
                    educationLevel = parsedLevel;
                }
            }

            // Create the course entity
            var course = new Course
            {
                CourseName = request.CourseName,
                CourseCode = courseCode,
                ProviderId = request.ProviderId,
                LearningType = request.ModeOfTraining,
                StartDate = request.CourseStartDate,
                EndDate = request.CourseEndDate,
                PaymentType = paymentType,
                FeeAmount = totalFee,
                FeePerCycle = feePerCycle,
                BillingCycle = request.BillingCycle,
                DurationByMonth = durationMonths,
                TermName = request.TermName ?? string.Empty,
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                EducationLevel = educationLevel,
                BillingDate = request.BillingDate,
                PaymentDue = request.PaymentDue
            };

            // Add course to repository
            var courseRepo = _unitOfWork.GetRepository<Course>();
            await courseRepo.InsertAsync(course);
            await _unitOfWork.SaveAsync();

            // Return the response
            return new CourseResponse
            {
                Id = course.Id,
                CourseName = course.CourseName,
                CourseCode = course.CourseCode,
                ProviderId = course.ProviderId,
                ProviderName = provider.Name,
                ModeOfTraining = course.LearningType,
                CourseStartDate = course.StartDate,
                CourseEndDate = course.EndDate,
                PaymentOption = course.PaymentType.ToString(),
                TotalFee = course.FeeAmount,
                BillingCycle = course.BillingCycle,
                FeePerCycle = course.FeePerCycle,
                TermName = course.TermName,
                Status = course.Status,
                CreatedAt = course.CreatedAt,
                EducationLevel = course.EducationLevel?.ToString(),
                BillingDate = course.BillingDate,
                PaymentDue = course.PaymentDue
            };
        }

        private int CalculateDurationInMonths(DateTime startDate, DateTime endDate)
        {
            int months = ((endDate.Year - startDate.Year) * 12) + endDate.Month - startDate.Month;
            
            // Add 1 if the end day is greater than or equal to start day
            if (endDate.Day >= startDate.Day)
            {
                months++;
            }

            return months > 0 ? months : 1;
        }

        private decimal CalculateTotalFeeFromCycle(decimal feePerCycle, string billingCycle, int durationMonths)
        {
            int numberOfCycles = billingCycle switch
            {
                "Monthly" => durationMonths,
                "Quarterly" => (int)Math.Ceiling(durationMonths / 3.0),
                "Biannually" => (int)Math.Ceiling(durationMonths / 6.0),
                "Yearly" => (int)Math.Ceiling(durationMonths / 12.0),
                _ => durationMonths
            };

            return feePerCycle * numberOfCycles;
        }

        private decimal CalculateFeePerCycle(decimal totalFee, string billingCycle, int durationMonths)
        {
            int numberOfCycles = billingCycle switch
            {
                "Monthly" => durationMonths,
                "Quarterly" => (int)Math.Ceiling(durationMonths / 3.0),
                "Biannually" => (int)Math.Ceiling(durationMonths / 6.0),
                "Yearly" => (int)Math.Ceiling(durationMonths / 12.0),
                _ => durationMonths
            };

            return numberOfCycles > 0 ? totalFee / numberOfCycles : totalFee;
        }

        private async Task<string> GenerateCourseCodeAsync(string courseName, decimal totalFee, string? billingCycle, string paymentOption)
        {
            // XXXX: First 4 characters of course name (uppercase, letters only)
            var nameChars = new string(courseName.Where(char.IsLetter).ToArray()).ToUpper();
            var prefix = nameChars.Length >= 4 ? nameChars.Substring(0, 4) : nameChars.PadRight(4, 'X');

            // YY: First two digits of total fee
            var feeString = ((int)totalFee).ToString();
            var feePrefix = feeString.Length >= 2 ? feeString.Substring(0, 2) : feeString.PadLeft(2, '0');

            // Z: Billing cycle code
            var cycleCode = paymentOption == "One-time" ? "O" : GetBillingCycleCode(billingCycle);

            // Build base course code: XXXXYYZ
            var baseCode = $"{prefix}{feePrefix}{cycleCode}";

            // n: Sequential number (3 digits) for courses with the same base code
            var courseRepo = _unitOfWork.GetRepository<Course>();
            var existingCoursesWithSameBase = await courseRepo.Entities
                .Where(c => c.CourseCode.StartsWith(baseCode))
                .Select(c => c.CourseCode)
                .ToListAsync();

            var sequentialNumber = 1;
            if (existingCoursesWithSameBase.Any())
            {
                // Extract sequential numbers from existing course codes
                var existingNumbers = existingCoursesWithSameBase
                    .Where(code => code.Length >= baseCode.Length + 4 && code[baseCode.Length] == '-')
                    .Select(code =>
                    {
                        var numberPart = code.Substring(baseCode.Length + 1);
                        return int.TryParse(numberPart, out var num) ? num : 0;
                    })
                    .Where(num => num > 0)
                    .ToList();

                if (existingNumbers.Any())
                {
                    sequentialNumber = existingNumbers.Max() + 1;
                }
            }

            return $"{baseCode}-{sequentialNumber:D3}";
        }

        private string GetBillingCycleCode(string? billingCycle)
        {
            if (string.IsNullOrWhiteSpace(billingCycle))
                return "O";

            return billingCycle.Trim().ToLowerInvariant() switch
            {
                "monthly" => "M",
                "quarterly" => "Q",
                "biannually" => "B",
                "yearly" => "A",
                "annually" => "A",
                _ => "O"
            };
        }

        public async Task<CourseDetailResponse?> GetCourseDetailAsync(string courseId, CancellationToken cancellationToken = default)
        {
            var courseRepo = _unitOfWork.GetRepository<Course>();

            var course = await courseRepo.FirstOrDefaultAsync(
                predicate: c => c.Id == courseId,
                include: query => query
                    .Include(c => c.Provider)
                    .Include(c => c.Enrollments.OrderByDescending(e => e.EnrollDate))
                        .ThenInclude(e => e.EducationAccount)
                            .ThenInclude(ea => ea!.AccountHolder)
                    .Include(c => c.Enrollments)
                        .ThenInclude(e => e.Invoices)
                            .ThenInclude(i => i.Transactions),
                cancellationToken: cancellationToken
            );

            if (course == null)
            {
                throw new NotFoundException($"Course with ID '{courseId}' not found.");
            }

            var enrolledStudents = (course.Enrollments ?? Enumerable.Empty<Enrollment>())
                .OrderByDescending(e => e.EnrollDate)
                .Select(e =>
                {
                    var totalPaid = (e.Invoices ?? Enumerable.Empty<Invoice>())
                        .SelectMany(i => i.Transactions ?? Enumerable.Empty<Transaction>())
                        .Sum(t => t.Amount);

                    var outstandingFee = Math.Max(0, course.FeeAmount - totalPaid);

                    return new EnrolledStudent(
                        e.EducationAccount?.AccountHolderId ?? string.Empty,
                        e.EducationAccountId,
                        e.EducationAccount?.AccountHolder?.FullName ?? "Unknown Student",
                        e.EducationAccount?.AccountHolder?.NRIC ?? "-",
                        totalPaid,
                        outstandingFee,
                        e.EnrollDate
                    );
                }).ToList();

            string? billingCycleDisplay = course.BillingCycle;
            decimal? feePerCycleDisplay = course.FeePerCycle;

            // Calculate feePerCycle if not stored but is a recurring course
            if (course.PaymentType == Domain.Enums.PaymentType.Recurring && !feePerCycleDisplay.HasValue)
            {
                if (!string.IsNullOrWhiteSpace(course.BillingCycle) && course.DurationByMonth > 0)
                {
                    var cycle = course.BillingCycle.Trim().ToLowerInvariant();

                    int numberOfCycles = cycle switch
                    {
                        "monthly" => course.DurationByMonth,
                        "quarterly" => (int)Math.Ceiling(course.DurationByMonth / 3.0),
                        "biannually" => (int)Math.Ceiling(course.DurationByMonth / 6.0),
                        "yearly" => (int)Math.Ceiling(course.DurationByMonth / 12.0),
                        _ => course.DurationByMonth
                    };

                    if (numberOfCycles > 0)
                    {
                        feePerCycleDisplay = Math.Round(
                            course.FeeAmount / numberOfCycles,
                            2,
                            MidpointRounding.AwayFromZero
                        );
                    }
                }
            }

            return new CourseDetailResponse(
                course.Id,
                course.CourseCode,
                course.CourseName,
                course.ProviderId ?? string.Empty,
                course.Provider?.Name ?? string.Empty,
                course.EducationLevel?.ToString() ?? string.Empty,
                course.LearningType,
                course.Status,
                course.StartDate,
                course.EndDate, 
                course.PaymentType.ToString(),
                billingCycleDisplay,
                feePerCycleDisplay,
                course.FeeAmount,
                enrolledStudents,
                course.BillingDate,
                course.PaymentDue
            );
        }

        public async Task UpdateCourseAsync(string courseId, UpdateCourseRequest request, CancellationToken cancellationToken = default)
        {
            var courseRepo = _unitOfWork.GetRepository<Course>();
            var providerRepo = _unitOfWork.GetRepository<Provider>();

            // Fetch course with provider information
            var course = await courseRepo.Entities
                .Include(c => c.Provider)
                .ThenInclude(p => p!.SchoolingLevels)
                .FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);

            if (course == null)
            {
                throw new NotFoundException("COURSE_NOT_FOUND", $"Course with ID {courseId} not found.");
            }

            // Update course name
            course.CourseName = request.CourseName;
            course.LearningType = request.LearningType;
            
            // Update EducationLevel if provided and validate it belongs to the provider
            if (!string.IsNullOrWhiteSpace(request.EducationLevel))
            {
                // Verify that the education level exists in the provider's schooling levels
                var providerHasLevel = course.Provider?.SchoolingLevels
                    ?.Any(sl => sl.Name.Equals(request.EducationLevel, StringComparison.OrdinalIgnoreCase)) ?? false;

                if (!providerHasLevel)
                {
                    throw new BadRequestException(
                        "INVALID_EDUCATION_LEVEL", 
                        $"The education level '{request.EducationLevel}' is not available for this course's provider."
                    );
                }

                // Parse the education level enum
                if (Enum.TryParse<EducationLevel>(request.EducationLevel, true, out var educationLevel))
                {
                    course.EducationLevel = educationLevel;
                }
                else
                {
                    throw new BadRequestException(
                        "INVALID_EDUCATION_LEVEL_FORMAT",
                        $"The education level '{request.EducationLevel}' is not a valid education level."
                    );
                }
            }
            
            // Update Status if provided
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                course.Status = request.Status;
            }

            courseRepo.Update(course);
            await _unitOfWork.SaveAsync();
        }

        public async Task<NonEnrolledAccountResponse> GetNonEnrolledAccountAsync(string courseId, CancellationToken cancellationToken)
        {
            var eduRepo = _unitOfWork.GetRepository<EducationAccount>();

            var nonEnrolledList = await eduRepo.Entities
                .Include(ec => ec.AccountHolder)
                .Where(ec => !ec.Enrollments.Any(e => e.CourseId == courseId))
                .ToListAsync();

            return new NonEnrolledAccountResponse
            {
                NonEnrolledAccounts = nonEnrolledList.Select(ne => new NonEnrolledAccountDetailResponse
                {
                    EducationAccountId = ne.Id,
                    FullName = ne.AccountHolder?.FullName ?? string.Empty,
                    NRIC = ne.AccountHolder?.NRIC ?? string.Empty
                }).ToList()
            };
        }

        public async Task DeleteCourseAsync(string courseId, CancellationToken cancellationToken = default)
        {
            var courseRepo = _unitOfWork.GetRepository<Course>();

            var course = await courseRepo.FirstOrDefaultAsync(
                predicate: c => c.Id == courseId,
                asTracking: true,
                cancellationToken: cancellationToken
            );

            if (course == null)
            {
                throw new NotFoundException("COURSE_NOT_FOUND", $"Course with ID {courseId} not found.");
            }

            course.DeletedAt = DateTime.UtcNow;

            await _unitOfWork.SaveAsync();
        }

        public async Task BulkRemoveEnrolledAccountAsync(BulkRemoveEnrolledAccountRequest request)
        {
            var enrollRepo = _unitOfWork.GetRepository<Enrollment>();
            var courseRepo = _unitOfWork.GetRepository<Course>();
            var invoiceRepo = _unitOfWork.GetRepository<Invoice>();

            if (string.IsNullOrWhiteSpace(request.CourseId))
                throw new NotFoundException("COURSE_NOT_FOUND", $"Course with ID {request.CourseId} not found.");

            // Get course by course ID
            var course = await courseRepo.FirstOrDefaultAsync(
                predicate: c => c.Id == request.CourseId
            );

            if (course == null)
                throw new NotFoundException("COURSE_NOT_FOUND", $"Course with ID {request.CourseId} not found.");

            if (request.EducationAccountIds == null || request.EducationAccountIds.Count == 0)
                return;

            // Get enrollments to be removed
            var enrollmentsToRemove = await enrollRepo.Entities
                .Where(e => e.CourseId == course.Id && request.EducationAccountIds.Contains(e.EducationAccountId))
                .Select(e => e.Id)
                .ToListAsync();

            if (enrollmentsToRemove.Any())
            {
                // Delete all outstanding invoices for these enrollments
                await invoiceRepo.Entities
                    .Where(i => enrollmentsToRemove.Contains(i.EnrollmentID) && i.Status == InvoiceStatus.Outstanding)
                    .ExecuteDeleteAsync();

                // Delete the enrollments
                await enrollRepo.Entities
                    .Where(e => enrollmentsToRemove.Contains(e.Id))
                    .ExecuteDeleteAsync();
            }
        }

        public async Task BulkEnrollAccountAsync(BulkEnrollAccountAsync request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.CourseId))
                throw new NotFoundException("COURSE_NOT_FOUND", $"Course with code {request.CourseId} not found.");

            if (request.AccountIds == null || request.AccountIds.Count == 0)
                return;

            var courseRepo = _unitOfWork.GetRepository<Course>();

            var course = await courseRepo.Entities
                .Include(c => c.Provider)
                .FirstOrDefaultAsync(c => c.Id == request.CourseId);

            if (course == null)
            {
                throw new NotFoundException("COURSE_NOT_FOUND", $"Course with ID {request.CourseId} not found.");
            }

            // Note: Provider EducationLevel is now managed via SchoolingLevels collection
            // var providerEducationLevel = course.Provider?.EducationLevel;

            var enrollmentRepo = _unitOfWork.GetRepository<Enrollment>();

            var existingEnrollments = await enrollmentRepo.ToListAsync(
                predicate: e => e.CourseId == course.Id && request.AccountIds.Contains(e.EducationAccountId)
            );

            var candidateStudentIds = request.AccountIds
                .Where(accountId => !existingEnrollments.Any(e => e.EducationAccountId == accountId))
                .Distinct()
                .ToList();

            if (!candidateStudentIds.Any())
                return;

            var accountRepo = _unitOfWork.GetRepository<EducationAccount>();

            var existingAccounts = await accountRepo.Entities.Where(ac => candidateStudentIds.Contains(ac.Id))
                .Include(ac => ac.AccountHolder)
                .ToListAsync();

            var validStudentIds = new List<string>();

            foreach (var account in existingAccounts)
            {
                validStudentIds.Add(account.Id);

                // Note: Provider EducationLevel logic removed since Provider uses SchoolingLevels collection now
                // if (!string.IsNullOrEmpty(providerEducationLevel))
                // {
                //     account.AccountHolder!.EducationLevel = Enum.Parse<Domain.Enums.EducationLevel>(providerEducationLevel);
                //     accountRepo.Update(account);
                // }
            }

            if (!validStudentIds.Any())
                return;

            var newEnrollments = validStudentIds.Select(educationId => new Enrollment
            {
                CourseId = course.Id,
                EducationAccountId = educationId,
                EnrollDate = DateTime.UtcNow,
                Status = PaymentStatus.Scheduled,
            }).ToList();

            await enrollmentRepo.InsertRangeAsync(newEnrollments);
            await _unitOfWork.SaveAsync();

            // Auto-generate invoices based on payment type and enrollment timing
            await GenerateEnrollmentInvoicesAsync(course, newEnrollments);
        }

        /// <summary>
        /// Generate invoices for new enrollments based on course payment type and enrollment timing.
        /// - One-time: Always create invoice immediately with enrollment date
        /// - Recurring: Only create invoice if enrolled AFTER billing date of current month
        /// </summary>
        private async Task GenerateEnrollmentInvoicesAsync(Course course, List<Enrollment> newEnrollments)
        {
            var invoiceRepo = _unitOfWork.GetRepository<Invoice>();
            var invoicesToCreate = new List<Invoice>();
            var enrollDate = DateTime.UtcNow;
            var paymentDue = course.PaymentDue ?? 30;

            foreach (var enrollment in newEnrollments)
            {
                Invoice? invoice = null;

                if (course.PaymentType == PaymentType.OneTime)
                {
                    // One-time: Always create invoice immediately
                    invoice = new Invoice
                    {
                        EnrollmentID = enrollment.Id,
                        Amount = course.FeeAmount,
                        PaymentType = PaymentType.OneTime,
                        BillingCycle = null,
                        BillingPeriodStart = course.StartDate,
                        BillingPeriodEnd = course.EndDate,
                        BillingDate = enrollDate,
                        PaymentDue = paymentDue,
                        DueDate = enrollDate.AddDays(paymentDue),
                        Status = InvoiceStatus.Outstanding
                    };
                }
                else if (course.PaymentType == PaymentType.Recurring && course.BillingDate.HasValue)
                {
                    // Recurring: Only create invoice if enrolled AFTER billing date of current month
                    var billingDayOfMonth = course.BillingDate.Value;
                    
                    // Check if enrollment is after billing date of current month
                    if (enrollDate.Day > billingDayOfMonth)
                    {
                        // Calculate current billing cycle
                        var (periodStart, periodEnd, _) = CalculateCurrentBillingCycle(
                            course.StartDate,
                            billingDayOfMonth,
                            course.BillingCycle!,
                            enrollDate,
                            course.EndDate);

                        // Only create if within course duration
                        if (enrollDate <= course.EndDate)
                        {
                            invoice = new Invoice
                            {
                                EnrollmentID = enrollment.Id,
                                Amount = course.FeePerCycle ?? 0,
                                PaymentType = PaymentType.Recurring,
                                BillingCycle = course.BillingCycle,
                                BillingPeriodStart = periodStart,
                                BillingPeriodEnd = periodEnd,
                                BillingDate = enrollDate, // Use enrollment date as billing date
                                PaymentDue = paymentDue,
                                DueDate = enrollDate.AddDays(paymentDue), // Due date = enroll date + payment due
                                Status = InvoiceStatus.Outstanding
                            };
                        }
                    }
                    // If enrolled BEFORE or ON billing date, no invoice is created now
                    // Invoice will be generated on the billing date via scheduled job
                }

                if (invoice != null)
                {
                    invoicesToCreate.Add(invoice);
                }
            }

            if (invoicesToCreate.Any())
            {
                await invoiceRepo.InsertRangeAsync(invoicesToCreate);
                await _unitOfWork.SaveAsync();
            }
        }

        private DateTime CalculateFirstBillingDate(DateTime courseStartDate, int billingDay)
        {
            if (courseStartDate.Day <= billingDay)
            {
                // Bill on the billing day of the start month
                var daysInMonth = DateTime.DaysInMonth(courseStartDate.Year, courseStartDate.Month);
                var actualDay = Math.Min(billingDay, daysInMonth);
                return new DateTime(courseStartDate.Year, courseStartDate.Month, actualDay);
            }
            else
            {
                // Bill on course start date (since it's after billing day)
                return courseStartDate;
            }
        }

        private (DateTime periodStart, DateTime periodEnd, DateTime billingDate) CalculateCurrentBillingCycle(
            DateTime courseStartDate, 
            int billingDay, 
            string billingCycle, 
            DateTime currentDate,
            DateTime courseEndDate)
        {
            var firstBillingDate = CalculateFirstBillingDate(courseStartDate, billingDay);
            
            int monthsPerCycle = billingCycle.ToLower() switch
            {
                "monthly" => 1,
                "quarterly" => 3,
                "biannually" => 6,
                "annually" => 12,
                _ => 1
            };

            // Find which cycle we're currently in
            var cycleStart = firstBillingDate;
            var cycleNumber = 0;

            while (cycleStart <= currentDate)
            {
                var nextCycleStart = cycleStart.AddMonths(monthsPerCycle);
                if (nextCycleStart > currentDate)
                {
                    // Found the current cycle
                    var periodStart = cycleStart;
                    var periodEnd = nextCycleStart.AddDays(-1);
                    
                    // Ensure period end doesn't exceed course end date
                    if (periodEnd > courseEndDate)
                        periodEnd = courseEndDate;

                    return (periodStart, periodEnd, cycleStart);
                }

                cycleStart = nextCycleStart;
                cycleNumber++;
            }

            // Fallback (shouldn't reach here if logic is correct)
            var fallbackEnd = firstBillingDate.AddMonths(monthsPerCycle).AddDays(-1);
            if (fallbackEnd > courseEndDate)
                fallbackEnd = courseEndDate;
                
            return (firstBillingDate, fallbackEnd, firstBillingDate);
        }
    }
}