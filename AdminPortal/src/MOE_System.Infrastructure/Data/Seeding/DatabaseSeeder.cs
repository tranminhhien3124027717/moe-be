using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MOE_System.Domain.Entities;
using MOE_System.Domain.Enums;

namespace MOE_System.Infrastructure.Data.Seeding;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(ApplicationDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Starting comprehensive database seeding...");
            
            // Clear all existing data first
            await ClearAllDataAsync();
            
            // Seed in order of dependencies
            await SeedAdminsAsync();
            await SeedAccountHoldersAsync();
            await SeedEducationAccountsAsync();
            await SeedSchoolingLevelsAsync();
            await SeedProvidersAsync();
            await SeedCoursesAsync();
            await SeedEnrollmentsAsync();
            await SeedInvoicesAsync();
            await SeedTransactionsAsync();
            await SeedHistoryOfChangesAsync();
            await SeedTopupRulesAsync();
            await SeedTopupRuleTargetsAsync();
            await SeedBatchExecutionsAsync();
            await SeedBatchRuleExecutionsAsync();
            await SeedTopupRuleAccountHoldersAsync();
            await SeedResidentsAsync(3);
            await SeedEducationLevelsAsync();
            await SeedSchoolingStatusesAsync();
            
            _logger.LogInformation("Database seeding completed successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private async Task ClearAllDataAsync()
    {
        _logger.LogInformation("Clearing all existing data from database...");

        // Delete in reverse order of dependencies to avoid FK constraint issues
        _context.BatchRuleExecutions.RemoveRange(_context.BatchRuleExecutions);
        _context.TopupRuleAccountHolders.RemoveRange(_context.TopupRuleAccountHolders);
        _context.TopupRuleTargets.RemoveRange(_context.TopupRuleTargets);
        _context.BatchExecutions.RemoveRange(_context.BatchExecutions);
        
        // Remove TopupExecutionSnapshots before TopupRules to avoid FK constraint
        if (_context.Model.FindEntityType(typeof(MOE_System.Domain.Entities.TopupExecutionSnapshot)) != null)
        {
            var topupExecutionSnapshots = _context.Set<MOE_System.Domain.Entities.TopupExecutionSnapshot>();
            _context.RemoveRange(topupExecutionSnapshots);
        }
        
        _context.TopupRules.RemoveRange(_context.TopupRules);
        _context.HistoryOfChanges.RemoveRange(_context.HistoryOfChanges);
        _context.Transactions.RemoveRange(_context.Transactions);
        _context.Invoices.RemoveRange(_context.Invoices);
        _context.Enrollments.RemoveRange(_context.Enrollments);
        _context.Courses.RemoveRange(_context.Courses);
        _context.Providers.RemoveRange(_context.Providers);
        _context.SchoolingLevels.RemoveRange(_context.SchoolingLevels);
        _context.EducationAccounts.RemoveRange(_context.EducationAccounts);
        _context.AccountHolders.RemoveRange(_context.AccountHolders);
        _context.Admins.RemoveRange(_context.Admins);
        _context.Set<Resident>().RemoveRange(_context.Set<Resident>());

        await _context.SaveChangesAsync();
        _logger.LogInformation("All data cleared successfully.");
    }

    private async Task SeedAdminsAsync()
    {
        _logger.LogInformation("Seeding admins...");

        var admins = new List<Admin>
        {
            new Admin { Id = "admin-001", UserName = "admin", Password = BCrypt.Net.BCrypt.HashPassword("Admin@123") }
        };

        await _context.Admins.AddRangeAsync(admins);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} admins", admins.Count);
    }

    private async Task SeedAccountHoldersAsync()
    {
        _logger.LogInformation("Seeding account holders...");

        var accounts = AccountSeedData.GetAccountsForSeeding();
        var accountHolders = accounts.Select(a => a.AccountHolder).ToList();

        await _context.AccountHolders.AddRangeAsync(accountHolders);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} account holders", accountHolders.Count);
    }

    private async Task SeedEducationAccountsAsync()
    {
        _logger.LogInformation("Seeding education accounts...");

        var accounts = AccountSeedData.GetAccountsForSeeding();
        var educationAccounts = accounts.Select(a => a.EducationAccount).ToList();

        await _context.EducationAccounts.AddRangeAsync(educationAccounts);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} education accounts", educationAccounts.Count);
    }

    private async Task SeedCoursesAsync()
    {
        _logger.LogInformation("Seeding courses...");

        var providers = await _context.Providers.ToListAsync();
        if (!providers.Any())
        {
            _logger.LogWarning("No providers found. Skipping course seeding.");
            return;
        }

        var courses = CourseSeedData.GetCoursesForSeeding();

        await _context.Courses.AddRangeAsync(courses);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} courses", courses.Count);
    }

    private async Task SeedEnrollmentsAsync()
    {
        _logger.LogInformation("Seeding enrollments for all users...");

        var enrollments = new List<Enrollment>
        {
            // Dave Dao - Enrollment 1: FullyPaid (ended course - Business Analytics Foundations)
            new Enrollment
            {
                Id = "enroll-001",
                CourseId = "course-0028", // Business Analytics Foundations - Feb 1, 2024 to Oct 31, 2024
                EducationAccountId = "458f6d8b-0e09-4db7-bb9a-10407a0bcb43", // Dave Dao
                EnrollDate = new DateTime(2024, 1, 1), // Day 1 = BillingDate(1) = no immediate invoice, first billing Feb 1
                Status = PaymentStatus.FullyPaid
            },
            // Dave Dao - Enrollment 2: Paid (current course - English Reading Programme, started but no billing yet)
            new Enrollment
            {
                Id = "enroll-002",
                CourseId = "course-0019", // English Reading Programme - Jan 15, 2026 to Oct 14, 2026
                EducationAccountId = "458f6d8b-0e09-4db7-bb9a-10407a0bcb43", // Dave Dao
                EnrollDate = new DateTime(2026, 1, 10),
                Status = PaymentStatus.Paid
            },
            // Dave Dao - Enrollment 3: Outstanding (current course - Business Administration Diploma)
            new Enrollment
            {
                Id = "enroll-003",
                CourseId = "course-0004", // Business Administration Diploma - Jan 8, 2026 to Dec 15, 2026
                EducationAccountId = "458f6d8b-0e09-4db7-bb9a-10407a0bcb43", // Dave Dao
                EnrollDate = new DateTime(2026, 1, 8), // Day 8 > BillingDate(5) = immediate invoice on enrollment date
                Status = PaymentStatus.Outstanding
            },
            // Dave Dao - Enrollment 4: Scheduled (future course - Computer Science Foundations)
            new Enrollment
            {
                Id = "enroll-004",
                CourseId = "course-0005", // Computer Science Foundations - Feb 10, 2026 to Nov 15, 2026
                EducationAccountId = "458f6d8b-0e09-4db7-bb9a-10407a0bcb43", // Dave Dao
                EnrollDate = new DateTime(2026, 1, 20),
                Status = PaymentStatus.Scheduled
            },
            // Kain Tran - Enrollment 1: FullyPaid (ended course - Business Analytics Foundations)
            new Enrollment
            {
                Id = "enroll-005",
                CourseId = "course-0028", // Business Analytics Foundations - Feb 1, 2024 to Oct 31, 2024
                EducationAccountId = "cb50d51a-4b0f-42b7-9994-f06e3c15ea1c", // Kain Tran
                EnrollDate = new DateTime(2024, 1, 1), // Day 1 = BillingDate(1) = no immediate invoice, first billing Feb 1
                Status = PaymentStatus.FullyPaid
            },
            // Kain Tran - Enrollment 2: Paid (current course - English Reading Programme)
            new Enrollment
            {
                Id = "enroll-006",
                CourseId = "course-0019", // English Reading Programme - Jan 15, 2026 to Oct 14, 2026
                EducationAccountId = "cb50d51a-4b0f-42b7-9994-f06e3c15ea1c", // Kain Tran
                EnrollDate = new DateTime(2026, 1, 10),
                Status = PaymentStatus.Paid
            },
            // Kain Tran - Enrollment 3: Outstanding (current course - Business Administration Diploma)
            new Enrollment
            {
                Id = "enroll-007",
                CourseId = "course-0004", // Business Administration Diploma - Jan 8, 2026 to Dec 15, 2026
                EducationAccountId = "cb50d51a-4b0f-42b7-9994-f06e3c15ea1c", // Kain Tran
                EnrollDate = new DateTime(2026, 1, 8), // Day 8 > BillingDate(5) = immediate invoice on enrollment date
                Status = PaymentStatus.Outstanding
            },
            // Kain Tran - Enrollment 4: Scheduled (future course - Computer Science Foundations)
            new Enrollment
            {
                Id = "enroll-008",
                CourseId = "course-0005", // Computer Science Foundations - Feb 10, 2026 to Nov 15, 2026
                EducationAccountId = "cb50d51a-4b0f-42b7-9994-f06e3c15ea1c", // Kain Tran
                EnrollDate = new DateTime(2026, 1, 20),
                Status = PaymentStatus.Scheduled
            },
            // Eric Nguyen - Enrollment 1: FullyPaid (ended course - Business Analytics Foundations)
            new Enrollment
            {
                Id = "enroll-009",
                CourseId = "course-0028", // Business Analytics Foundations - Feb 1, 2024 to Oct 31, 2024
                EducationAccountId = "2793d115-7242-46c9-86e2-93aa565ff435", // Eric Nguyen
                EnrollDate = new DateTime(2024, 1, 1), // Day 1 = BillingDate(1) = no immediate invoice, first billing Feb 1
                Status = PaymentStatus.FullyPaid
            },
            // Eric Nguyen - Enrollment 2: Paid (current course - English Reading Programme)
            new Enrollment
            {
                Id = "enroll-010",
                CourseId = "course-0019", // English Reading Programme - Jan 15, 2026 to Oct 14, 2026
                EducationAccountId = "2793d115-7242-46c9-86e2-93aa565ff435", // Eric Nguyen
                EnrollDate = new DateTime(2026, 1, 10),
                Status = PaymentStatus.Paid
            },
            // Eric Nguyen - Enrollment 3: Outstanding (current course - Business Administration Diploma)
            new Enrollment
            {
                Id = "enroll-011",
                CourseId = "course-0004", // Business Administration Diploma - Jan 8, 2026 to Dec 15, 2026
                EducationAccountId = "2793d115-7242-46c9-86e2-93aa565ff435", // Eric Nguyen
                EnrollDate = new DateTime(2026, 1, 8), // Day 8 > BillingDate(5) = immediate invoice on enrollment date
                Status = PaymentStatus.Outstanding
            },
            // Eric Nguyen - Enrollment 4: Scheduled (future course - Computer Science Foundations)
            new Enrollment
            {
                Id = "enroll-012",
                CourseId = "course-0005", // Computer Science Foundations - Feb 10, 2026 to Nov 15, 2026
                EducationAccountId = "2793d115-7242-46c9-86e2-93aa565ff435", // Eric Nguyen
                EnrollDate = new DateTime(2026, 1, 20),
                Status = PaymentStatus.Scheduled
            },
            // === Ryan Le - Permanent Resident (Balance = 0, pays by CreditCard) ===
            // Ryan Le - Enrollment 1: FullyPaid (ended course - Business Analytics Foundations)
            new Enrollment
            {
                Id = "enroll-013",
                CourseId = "course-0028", // Business Analytics Foundations - Feb 1, 2024 to Oct 31, 2024
                EducationAccountId = "a2e9b432-fd9e-428b-9f36-56c7e1a0b658", // Ryan Le
                EnrollDate = new DateTime(2024, 1, 1), // Day 1 = BillingDate(1) = no immediate invoice, first billing Feb 1
                Status = PaymentStatus.FullyPaid
            },
            // Ryan Le - Enrollment 2: Paid (current course - English Reading Programme)
            new Enrollment
            {
                Id = "enroll-014",
                CourseId = "course-0019", // English Reading Programme - Jan 15, 2026 to Oct 14, 2026
                EducationAccountId = "a2e9b432-fd9e-428b-9f36-56c7e1a0b658", // Ryan Le
                EnrollDate = new DateTime(2026, 1, 10),
                Status = PaymentStatus.Paid
            },
            // Ryan Le - Enrollment 3: Outstanding (current course - Business Administration Diploma)
            new Enrollment
            {
                Id = "enroll-015",
                CourseId = "course-0004", // Business Administration Diploma - Jan 8, 2026 to Dec 15, 2026
                EducationAccountId = "a2e9b432-fd9e-428b-9f36-56c7e1a0b658", // Ryan Le
                EnrollDate = new DateTime(2026, 1, 8), // Day 8 > BillingDate(5) = immediate invoice on enrollment date
                Status = PaymentStatus.Outstanding
            },
            // Ryan Le - Enrollment 4: Scheduled (future course - Computer Science Foundations)
            new Enrollment
            {
                Id = "enroll-016",
                CourseId = "course-0005", // Computer Science Foundations - Feb 10, 2026 to Nov 15, 2026
                EducationAccountId = "a2e9b432-fd9e-428b-9f36-56c7e1a0b658", // Ryan Le
                EnrollDate = new DateTime(2026, 1, 20),
                Status = PaymentStatus.Scheduled
            },
            // === Suki Nguyen - Non-Resident (Balance = 0, pays by CreditCard) ===
            // Suki Nguyen - Enrollment 1: FullyPaid (ended course - Business Analytics Foundations)
            new Enrollment
            {
                Id = "enroll-017",
                CourseId = "course-0028", // Business Analytics Foundations - Feb 1, 2024 to Oct 31, 2024
                EducationAccountId = "c7d8e9f0-1a2b-3c4d-5e6f-7a8b9c0d1e2f", // Suki Nguyen
                EnrollDate = new DateTime(2024, 1, 1), // Day 1 = BillingDate(1) = no immediate invoice, first billing Feb 1
                Status = PaymentStatus.FullyPaid
            },
            // Suki Nguyen - Enrollment 2: Paid (current course - Integrated STEM Programme)
            new Enrollment
            {
                Id = "enroll-018",
                CourseId = "course-0007", // Integrated STEM Programme - Jan 31, 2026 to Feb 17, 2027 (Secondary)
                EducationAccountId = "c7d8e9f0-1a2b-3c4d-5e6f-7a8b9c0d1e2f", // Suki Nguyen
                EnrollDate = new DateTime(2026, 1, 10),
                Status = PaymentStatus.Paid
            },
            // Suki Nguyen - Enrollment 3: Outstanding (current course - Secondary Class for Design & Technology)
            new Enrollment
            {
                Id = "enroll-019",
                CourseId = "course-0036", // Secondary Class for Design & Technology - Feb 1, 2026 to May 31, 2026
                EducationAccountId = "c7d8e9f0-1a2b-3c4d-5e6f-7a8b9c0d1e2f", // Suki Nguyen
                EnrollDate = new DateTime(2026, 1, 5), // Day 5 = BillingDate(5) = no immediate invoice, wait for billing date
                Status = PaymentStatus.Outstanding
            },
            // Suki Nguyen - Enrollment 4: Scheduled (future course - Secondary Literature)
            new Enrollment
            {
                Id = "enroll-020",
                CourseId = "course-0042", // Secondary Literature - Jan 1, 2026 to Dec 31, 2026
                EducationAccountId = "c7d8e9f0-1a2b-3c4d-5e6f-7a8b9c0d1e2f", // Suki Nguyen
                EnrollDate = new DateTime(2026, 1, 20),
                Status = PaymentStatus.Scheduled
            }
        };

        await _context.Enrollments.AddRangeAsync(enrollments);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} enrollments for all users", enrollments.Count);
    }

    private async Task SeedInvoicesAsync()
    {
        _logger.LogInformation("Seeding invoices for all users...");

        var invoices = new List<Invoice>
        {
            // === Dave Dao - Enrollment 1 (Business Analytics Foundations - FullyPaid): 3 quarterly cycles ===
            // Cycle 1: Feb 1, 2024 - Apr 30, 2024
            new Invoice
            {
                Id = "inv-001",
                EnrollmentID = "enroll-001",
                Amount = 100.00m,
                PaymentType = PaymentType.Recurring,
                BillingCycle = "Quarterly",
                BillingPeriodStart = new DateTime(2024, 2, 1),
                BillingPeriodEnd = new DateTime(2024, 4, 30),
                BillingDate = new DateTime(2024, 2, 1),
                PaymentDue = 30,
                DueDate = new DateTime(2024, 3, 2),
                Status = InvoiceStatus.Paid
            },
            // Cycle 2: May 1, 2024 - Jul 31, 2024
            new Invoice
            {
                Id = "inv-002",
                EnrollmentID = "enroll-001",
                Amount = 100.00m,
                PaymentType = PaymentType.Recurring,
                BillingCycle = "Quarterly",
                BillingPeriodStart = new DateTime(2024, 5, 1),
                BillingPeriodEnd = new DateTime(2024, 7, 31),
                BillingDate = new DateTime(2024, 5, 1),
                PaymentDue = 30,
                DueDate = new DateTime(2024, 5, 31),
                Status = InvoiceStatus.Paid
            },
            // Cycle 3: Aug 1, 2024 - Oct 31, 2024 (course ends)
            new Invoice
            {
                Id = "inv-003",
                EnrollmentID = "enroll-001",
                Amount = 100.00m,
                PaymentType = PaymentType.Recurring,
                BillingCycle = "Quarterly",
                BillingPeriodStart = new DateTime(2024, 8, 1),
                BillingPeriodEnd = new DateTime(2024, 10, 31),
                BillingDate = new DateTime(2024, 8, 1),
                PaymentDue = 30,
                DueDate = new DateTime(2024, 8, 31),
                Status = InvoiceStatus.Paid
            },
            // === Dave Dao - Enrollment 3 (Business Administration Diploma): 1 outstanding invoice ===
            new Invoice
            {
                Id = "inv-004",
                EnrollmentID = "enroll-003",
                Amount = 300.00m,
                PaymentType = PaymentType.Recurring,
                BillingCycle = "Monthly",
                BillingPeriodStart = new DateTime(2026, 1, 8),
                BillingPeriodEnd = new DateTime(2026, 2, 7),
                BillingDate = new DateTime(2026, 1, 8),
                PaymentDue = 30,
                DueDate = new DateTime(2026, 2, 7),
                Status = InvoiceStatus.Outstanding
            },
            // === Kain Tran - Enrollment 5 (Business Analytics Foundations - FullyPaid): 3 quarterly cycles ===
            new Invoice
            {
                Id = "inv-005",
                EnrollmentID = "enroll-005",
                Amount = 100.00m,
                PaymentType = PaymentType.Recurring,
                BillingCycle = "Quarterly",
                BillingPeriodStart = new DateTime(2024, 2, 1),
                BillingPeriodEnd = new DateTime(2024, 4, 30),
                BillingDate = new DateTime(2024, 2, 1),
                PaymentDue = 30,
                DueDate = new DateTime(2024, 3, 2),
                Status = InvoiceStatus.Paid
            },
            new Invoice
            {
                Id = "inv-006",
                EnrollmentID = "enroll-005",
                Amount = 100.00m,
                PaymentType = PaymentType.Recurring,
                BillingCycle = "Quarterly",
                BillingPeriodStart = new DateTime(2024, 5, 1),
                BillingPeriodEnd = new DateTime(2024, 7, 31),
                BillingDate = new DateTime(2024, 5, 1),
                PaymentDue = 30,
                DueDate = new DateTime(2024, 5, 31),
                Status = InvoiceStatus.Paid
            },
            new Invoice
            {
                Id = "inv-007",
                EnrollmentID = "enroll-005",
                Amount = 100.00m,
                PaymentType = PaymentType.Recurring,
                BillingCycle = "Quarterly",
                BillingPeriodStart = new DateTime(2024, 8, 1),
                BillingPeriodEnd = new DateTime(2024, 10, 31),
                BillingDate = new DateTime(2024, 8, 1),
                PaymentDue = 30,
                DueDate = new DateTime(2024, 8, 31),
                Status = InvoiceStatus.Paid
            },
            // === Kain Tran - Enrollment 7 (Business Administration Diploma): 1 outstanding invoice ===
            new Invoice
            {
                Id = "inv-008",
                EnrollmentID = "enroll-007",
                Amount = 300.00m,
                PaymentType = PaymentType.Recurring,
                BillingCycle = "Monthly",
                BillingPeriodStart = new DateTime(2026, 1, 8),
                BillingPeriodEnd = new DateTime(2026, 2, 7),
                BillingDate = new DateTime(2026, 1, 8),
                PaymentDue = 30,
                DueDate = new DateTime(2026, 2, 7),
                Status = InvoiceStatus.Outstanding
            },
            // === Eric Nguyen - Enrollment 9 (Business Analytics Foundations - FullyPaid): 3 quarterly cycles ===
            new Invoice
            {
                Id = "inv-009",
                EnrollmentID = "enroll-009",
                Amount = 100.00m,
                PaymentType = PaymentType.Recurring,
                BillingCycle = "Quarterly",
                BillingPeriodStart = new DateTime(2024, 2, 1),
                BillingPeriodEnd = new DateTime(2024, 4, 30),
                BillingDate = new DateTime(2024, 2, 1),
                PaymentDue = 30,
                DueDate = new DateTime(2024, 3, 2),
                Status = InvoiceStatus.Paid
            },
            new Invoice
            {
                Id = "inv-010",
                EnrollmentID = "enroll-009",
                Amount = 100.00m,
                PaymentType = PaymentType.Recurring,
                BillingCycle = "Quarterly",
                BillingPeriodStart = new DateTime(2024, 5, 1),
                BillingPeriodEnd = new DateTime(2024, 7, 31),
                BillingDate = new DateTime(2024, 5, 1),
                PaymentDue = 30,
                DueDate = new DateTime(2024, 5, 31),
                Status = InvoiceStatus.Paid
            },
            new Invoice
            {
                Id = "inv-011",
                EnrollmentID = "enroll-009",
                Amount = 100.00m,
                PaymentType = PaymentType.Recurring,
                BillingCycle = "Quarterly",
                BillingPeriodStart = new DateTime(2024, 8, 1),
                BillingPeriodEnd = new DateTime(2024, 10, 31),
                BillingDate = new DateTime(2024, 8, 1),
                PaymentDue = 30,
                DueDate = new DateTime(2024, 8, 31),
                Status = InvoiceStatus.Paid
            },
            // === Eric Nguyen - Enrollment 11 (Business Administration Diploma): 1 outstanding invoice ===
            new Invoice
            {
                Id = "inv-012",
                EnrollmentID = "enroll-011",
                Amount = 300.00m,
                PaymentType = PaymentType.Recurring,
                BillingCycle = "Monthly",
                BillingPeriodStart = new DateTime(2026, 1, 8),
                BillingPeriodEnd = new DateTime(2026, 2, 7),
                BillingDate = new DateTime(2026, 1, 8),
                PaymentDue = 30,
                DueDate = new DateTime(2026, 2, 7),
                Status = InvoiceStatus.Outstanding
            },
            // === Ryan Le - Enrollment 13 (Business Analytics Foundations - FullyPaid): 3 quarterly cycles ===
            new Invoice
            {
                Id = "inv-013",
                EnrollmentID = "enroll-013",
                Amount = 100.00m,
                PaymentType = PaymentType.Recurring,
                BillingCycle = "Quarterly",
                BillingPeriodStart = new DateTime(2024, 2, 1),
                BillingPeriodEnd = new DateTime(2024, 4, 30),
                BillingDate = new DateTime(2024, 2, 1),
                PaymentDue = 30,
                DueDate = new DateTime(2024, 3, 2),
                Status = InvoiceStatus.Paid
            },
            new Invoice
            {
                Id = "inv-014",
                EnrollmentID = "enroll-013",
                Amount = 100.00m,
                PaymentType = PaymentType.Recurring,
                BillingCycle = "Quarterly",
                BillingPeriodStart = new DateTime(2024, 5, 1),
                BillingPeriodEnd = new DateTime(2024, 7, 31),
                BillingDate = new DateTime(2024, 5, 1),
                PaymentDue = 30,
                DueDate = new DateTime(2024, 5, 31),
                Status = InvoiceStatus.Paid
            },
            new Invoice
            {
                Id = "inv-015",
                EnrollmentID = "enroll-013",
                Amount = 100.00m,
                PaymentType = PaymentType.Recurring,
                BillingCycle = "Quarterly",
                BillingPeriodStart = new DateTime(2024, 8, 1),
                BillingPeriodEnd = new DateTime(2024, 10, 31),
                BillingDate = new DateTime(2024, 8, 1),
                PaymentDue = 30,
                DueDate = new DateTime(2024, 8, 31),
                Status = InvoiceStatus.Paid
            },
            // === Ryan Le - Enrollment 15 (Business Administration Diploma): 1 outstanding invoice ===
            new Invoice
            {
                Id = "inv-016",
                EnrollmentID = "enroll-015",
                Amount = 300.00m,
                PaymentType = PaymentType.Recurring,
                BillingCycle = "Monthly",
                BillingPeriodStart = new DateTime(2026, 1, 8),
                BillingPeriodEnd = new DateTime(2026, 2, 7),
                BillingDate = new DateTime(2026, 1, 8),
                PaymentDue = 30,
                DueDate = new DateTime(2026, 2, 7),
                Status = InvoiceStatus.Outstanding
            },
            // === Suki Nguyen - Enrollment 17 (Business Analytics Foundations - FullyPaid): 3 quarterly cycles ===
            new Invoice
            {
                Id = "inv-017",
                EnrollmentID = "enroll-017",
                Amount = 100.00m,
                PaymentType = PaymentType.Recurring,
                BillingCycle = "Quarterly",
                BillingPeriodStart = new DateTime(2024, 2, 1),
                BillingPeriodEnd = new DateTime(2024, 4, 30),
                BillingDate = new DateTime(2024, 2, 1),
                PaymentDue = 30,
                DueDate = new DateTime(2024, 3, 2),
                Status = InvoiceStatus.Paid
            },
            new Invoice
            {
                Id = "inv-018",
                EnrollmentID = "enroll-017",
                Amount = 100.00m,
                PaymentType = PaymentType.Recurring,
                BillingCycle = "Quarterly",
                BillingPeriodStart = new DateTime(2024, 5, 1),
                BillingPeriodEnd = new DateTime(2024, 7, 31),
                BillingDate = new DateTime(2024, 5, 1),
                PaymentDue = 30,
                DueDate = new DateTime(2024, 5, 31),
                Status = InvoiceStatus.Paid
            },
            new Invoice
            {
                Id = "inv-019",
                EnrollmentID = "enroll-017",
                Amount = 100.00m,
                PaymentType = PaymentType.Recurring,
                BillingCycle = "Quarterly",
                BillingPeriodStart = new DateTime(2024, 8, 1),
                BillingPeriodEnd = new DateTime(2024, 10, 31),
                BillingDate = new DateTime(2024, 8, 1),
                PaymentDue = 30,
                DueDate = new DateTime(2024, 8, 31),
                Status = InvoiceStatus.Paid
            },
            // === Suki Nguyen - Enrollment 19 (Secondary Class for Design & Technology): 1 outstanding invoice ===
            new Invoice
            {
                Id = "inv-020",
                EnrollmentID = "enroll-019",
                Amount = 87.50m,
                PaymentType = PaymentType.Recurring,
                BillingCycle = "Monthly",
                BillingPeriodStart = new DateTime(2026, 2, 1),
                BillingPeriodEnd = new DateTime(2026, 2, 28),
                BillingDate = new DateTime(2026, 2, 5),
                PaymentDue = 14,
                DueDate = new DateTime(2026, 2, 19),
                Status = InvoiceStatus.Outstanding
            },
        };

        await _context.Invoices.AddRangeAsync(invoices);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} invoices for all users", invoices.Count);
    }

    private async Task SeedTransactionsAsync()
    {
        _logger.LogInformation("Seeding transactions for all users...");

        var transactions = new List<Transaction>
        {
            // === Dave Dao - Business Analytics Foundations: 3 paid invoices ===
            new Transaction
            {
                Id = "txn-001",
                InvoiceId = "inv-001",
                Amount = 100.00m,
                TransactionAt = new DateTime(2024, 2, 15),
                PaymentMethod = PaymentMethod.AccountBalance,
                Status = TransactionStatus.Success,
                Description = "Account balance payment - Business Analytics Foundations Feb 2024"
            },
            new Transaction
            {
                Id = "txn-002",
                InvoiceId = "inv-002",
                Amount = 100.00m,
                TransactionAt = new DateTime(2024, 5, 20),
                PaymentMethod = PaymentMethod.AccountBalance,
                Status = TransactionStatus.Success,
                Description = "Account balance payment - Business Analytics Foundations May 2024"
            },
            new Transaction
            {
                Id = "txn-003",
                InvoiceId = "inv-003",
                Amount = 100.00m,
                TransactionAt = new DateTime(2024, 8, 12),
                PaymentMethod = PaymentMethod.AccountBalance,
                Status = TransactionStatus.Success,
                Description = "Account balance payment - Business Analytics Foundations Aug 2024"
            },
            // === Kain Tran - Business Analytics Foundations: 3 paid invoices ===
            new Transaction
            {
                Id = "txn-004",
                InvoiceId = "inv-005",
                Amount = 100.00m,
                TransactionAt = new DateTime(2024, 2, 15),
                PaymentMethod = PaymentMethod.AccountBalance,
                Status = TransactionStatus.Success,
                Description = "Account balance payment - Business Analytics Foundations Feb 2024"
            },
            new Transaction
            {
                Id = "txn-005",
                InvoiceId = "inv-006",
                Amount = 100.00m,
                TransactionAt = new DateTime(2024, 5, 20),
                PaymentMethod = PaymentMethod.AccountBalance,
                Status = TransactionStatus.Success,
                Description = "Account balance payment - Business Analytics Foundations May 2024"
            },
            new Transaction
            {
                Id = "txn-006",
                InvoiceId = "inv-007",
                Amount = 100.00m,
                TransactionAt = new DateTime(2024, 8, 12),
                PaymentMethod = PaymentMethod.AccountBalance,
                Status = TransactionStatus.Success,
                Description = "Account balance payment - Business Analytics Foundations Aug 2024"
            },
            // === Eric Nguyen - Business Analytics Foundations: 3 paid invoices ===
            new Transaction
            {
                Id = "txn-007",
                InvoiceId = "inv-009",
                Amount = 100.00m,
                TransactionAt = new DateTime(2024, 2, 15),
                PaymentMethod = PaymentMethod.AccountBalance,
                Status = TransactionStatus.Success,
                Description = "Account balance payment - Business Analytics Foundations Feb 2024"
            },
            new Transaction
            {
                Id = "txn-008",
                InvoiceId = "inv-010",
                Amount = 100.00m,
                TransactionAt = new DateTime(2024, 5, 20),
                PaymentMethod = PaymentMethod.AccountBalance,
                Status = TransactionStatus.Success,
                Description = "Account balance payment - Business Analytics Foundations May 2024"
            },
            new Transaction
            {
                Id = "txn-009",
                InvoiceId = "inv-011",
                Amount = 100.00m,
                TransactionAt = new DateTime(2024, 8, 12),
                PaymentMethod = PaymentMethod.AccountBalance,
                Status = TransactionStatus.Success,
                Description = "Account balance payment - Business Analytics Foundations Aug 2024"
            },
            // === Ryan Le - Business Analytics Foundations: 3 paid invoices (CreditCard - no balance) ===
            new Transaction
            {
                Id = "txn-010",
                InvoiceId = "inv-013",
                Amount = 100.00m,
                TransactionAt = new DateTime(2024, 2, 15),
                PaymentMethod = PaymentMethod.CreditDebitCard,
                Status = TransactionStatus.Success,
                Description = "Credit card payment - Business Analytics Foundations Feb 2024"
            },
            new Transaction
            {
                Id = "txn-011",
                InvoiceId = "inv-014",
                Amount = 100.00m,
                TransactionAt = new DateTime(2024, 5, 20),
                PaymentMethod = PaymentMethod.BankTransfer,
                Status = TransactionStatus.Success,
                Description = "Credit card payment - Business Analytics Foundations May 2024"
            },
            new Transaction
            {
                Id = "txn-012",
                InvoiceId = "inv-015",
                Amount = 100.00m,
                TransactionAt = new DateTime(2024, 8, 12),
                PaymentMethod = PaymentMethod.CreditDebitCard,
                Status = TransactionStatus.Success,
                Description = "Credit card payment - Business Analytics Foundations Aug 2024"
            },
            // === Suki Nguyen - Business Analytics Foundations: 3 paid invoices (CreditCard - no balance) ===
            new Transaction
            {
                Id = "txn-013",
                InvoiceId = "inv-017",
                Amount = 100.00m,
                TransactionAt = new DateTime(2024, 2, 15),
                PaymentMethod = PaymentMethod.CreditDebitCard,
                Status = TransactionStatus.Success,
                Description = "Credit card payment - Business Analytics Foundations Feb 2024"
            },
            new Transaction
            {
                Id = "txn-014",
                InvoiceId = "inv-018",
                Amount = 100.00m,
                TransactionAt = new DateTime(2024, 5, 20),
                PaymentMethod = PaymentMethod.CreditDebitCard,
                Status = TransactionStatus.Success,
                Description = "Credit card payment - Business Analytics Foundations May 2024"
            },
            new Transaction
            {
                Id = "txn-015",
                InvoiceId = "inv-019",
                Amount = 100.00m,
                TransactionAt = new DateTime(2024, 8, 12),
                PaymentMethod = PaymentMethod.BankTransfer,
                Status = TransactionStatus.Success,
                Description = "Credit card payment - Business Analytics Foundations Aug 2024"
            }
        };

        await _context.Transactions.AddRangeAsync(transactions);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} transactions for all users", transactions.Count);
    }

    private async Task SeedHistoryOfChangesAsync()
    {
        _logger.LogInformation("Seeding history of changes for all users...");

        var histories = new List<HistoryOfChange>
        {
            // Dave Dao's initial top-up (Singapore Citizen = $5000)
            new HistoryOfChange
            {
                Id = "hist-001",
                EducationAccountId = "458f6d8b-0e09-4db7-bb9a-10407a0bcb43",
                ReferenceId = "topup-001",
                Amount = 5000.00m,
                Type = ChangeType.TopUp,
                BalanceBefore = 0m,
                BalanceAfter = 5000.00m,
                CreatedAt = new DateTime(2024, 1, 20, 9, 0, 0),
                Description = "Initial account funding - Welcome bonus for Singapore Citizen"
            },
            // Dave Dao's first AccountBalance payment (Business Analytics Cycle 1)
            new HistoryOfChange
            {
                Id = "hist-002",
                EducationAccountId = "458f6d8b-0e09-4db7-bb9a-10407a0bcb43",
                ReferenceId = "inv-001",
                Amount = -100.00m,
                Type = ChangeType.CoursePayment,
                BalanceBefore = 5000.00m,
                BalanceAfter = 4900.00m,
                CreatedAt = new DateTime(2024, 2, 15, 10, 30, 0),
                Description = "Course Payment: Business Analytics Foundations - Feb 2024"
            },
            // Dave Dao's second AccountBalance payment (Business Analytics Cycle 2)
            new HistoryOfChange
            {
                Id = "hist-003",
                EducationAccountId = "458f6d8b-0e09-4db7-bb9a-10407a0bcb43",
                ReferenceId = "inv-002",
                Amount = -100.00m,
                Type = ChangeType.CoursePayment,
                BalanceBefore = 4900.00m,
                BalanceAfter = 4800.00m,
                CreatedAt = new DateTime(2024, 5, 20, 11, 15, 0),
                Description = "Course Payment: Business Analytics Foundations - May 2024"
            },
            // Dave Dao's third AccountBalance payment (Business Analytics Cycle 3)
            new HistoryOfChange
            {
                Id = "hist-004",
                EducationAccountId = "458f6d8b-0e09-4db7-bb9a-10407a0bcb43",
                ReferenceId = "inv-003",
                Amount = -100.00m,
                Type = ChangeType.CoursePayment,
                BalanceBefore = 4800.00m,
                BalanceAfter = 4700.00m,
                CreatedAt = new DateTime(2024, 8, 12, 14, 20, 0),
                Description = "Course Payment: Business Analytics Foundations - Aug 2024"
            },
            // Kain Tran's initial top-up (Singapore Citizen = $5000)
            new HistoryOfChange
            {
                Id = "hist-005",
                EducationAccountId = "cb50d51a-4b0f-42b7-9994-f06e3c15ea1c",
                ReferenceId = "topup-002",
                Amount = 5000.00m,
                Type = ChangeType.TopUp,
                BalanceBefore = 0m,
                BalanceAfter = 5000.00m,
                CreatedAt = new DateTime(2024, 1, 20, 9, 0, 0),
                Description = "Initial account funding - Welcome bonus for Singapore Citizen"
            },
            // Kain Tran's first AccountBalance payment (Business Analytics Cycle 1)
            new HistoryOfChange
            {
                Id = "hist-006",
                EducationAccountId = "cb50d51a-4b0f-42b7-9994-f06e3c15ea1c",
                ReferenceId = "inv-005",
                Amount = -100.00m,
                Type = ChangeType.CoursePayment,
                BalanceBefore = 5000.00m,
                BalanceAfter = 4900.00m,
                CreatedAt = new DateTime(2024, 2, 15, 10, 30, 0),
                Description = "Course Payment: Business Analytics Foundations - Feb 2024"
            },
            // Kain Tran's second AccountBalance payment (Business Analytics Cycle 2)
            new HistoryOfChange
            {
                Id = "hist-007",
                EducationAccountId = "cb50d51a-4b0f-42b7-9994-f06e3c15ea1c",
                ReferenceId = "inv-006",
                Amount = -100.00m,
                Type = ChangeType.CoursePayment,
                BalanceBefore = 4900.00m,
                BalanceAfter = 4800.00m,
                CreatedAt = new DateTime(2024, 5, 20, 11, 15, 0),
                Description = "Course Payment: Business Analytics Foundations - May 2024"
            },
            // Kain Tran's third AccountBalance payment (Business Analytics Cycle 3)
            new HistoryOfChange
            {
                Id = "hist-008",
                EducationAccountId = "cb50d51a-4b0f-42b7-9994-f06e3c15ea1c",
                ReferenceId = "inv-007",
                Amount = -100.00m,
                Type = ChangeType.CoursePayment,
                BalanceBefore = 4800.00m,
                BalanceAfter = 4700.00m,
                CreatedAt = new DateTime(2024, 8, 12, 14, 20, 0),
                Description = "Course Payment: Business Analytics Foundations - Aug 2024"
            },
            // Eric Nguyen's initial top-up (Singapore Citizen = $5000)
            new HistoryOfChange
            {
                Id = "hist-009",
                EducationAccountId = "2793d115-7242-46c9-86e2-93aa565ff435",
                ReferenceId = "topup-003",
                Amount = 5000.00m,
                Type = ChangeType.TopUp,
                BalanceBefore = 0m,
                BalanceAfter = 5000.00m,
                CreatedAt = new DateTime(2024, 1, 20, 9, 0, 0),
                Description = "Initial account funding - Welcome bonus for Singapore Citizen"
            },
            // Eric Nguyen's first AccountBalance payment (Business Analytics Cycle 1)
            new HistoryOfChange
            {
                Id = "hist-010",
                EducationAccountId = "2793d115-7242-46c9-86e2-93aa565ff435",
                ReferenceId = "inv-009",
                Amount = -100.00m,
                Type = ChangeType.CoursePayment,
                BalanceBefore = 5000.00m,
                BalanceAfter = 4900.00m,
                CreatedAt = new DateTime(2024, 2, 15, 10, 30, 0),
                Description = "Course Payment: Business Analytics Foundations - Feb 2024"
            },
            // Eric Nguyen's second AccountBalance payment (Business Analytics Cycle 2)
            new HistoryOfChange
            {
                Id = "hist-011",
                EducationAccountId = "2793d115-7242-46c9-86e2-93aa565ff435",
                ReferenceId = "inv-010",
                Amount = -100.00m,
                Type = ChangeType.CoursePayment,
                BalanceBefore = 4900.00m,
                BalanceAfter = 4800.00m,
                CreatedAt = new DateTime(2024, 5, 20, 11, 15, 0),
                Description = "Course Payment: Business Analytics Foundations - May 2024"
            },
            // Eric Nguyen's third AccountBalance payment (Business Analytics Cycle 3)
            new HistoryOfChange
            {
                Id = "hist-012",
                EducationAccountId = "2793d115-7242-46c9-86e2-93aa565ff435",
                ReferenceId = "inv-011",
                Amount = -100.00m,
                Type = ChangeType.CoursePayment,
                BalanceBefore = 4800.00m,
                BalanceAfter = 4700.00m,
                CreatedAt = new DateTime(2024, 8, 12, 14, 20, 0),
                Description = "Course Payment: Business Analytics Foundations - Aug 2024"
            }
        };

        await _context.HistoryOfChanges.AddRangeAsync(histories);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} history records for all users", histories.Count);
    }

    private async Task SeedTopupRulesAsync()
    {
        _logger.LogInformation("Seeding topup rules with updated schema...");

        if (await _context.TopupRules.AnyAsync()) return;

        var topupRules = new List<TopupRule>
        {
            // 1. Individual Topup - for Tim
            new TopupRule
            {
                Id = "rule-001",
                RuleName = "Low Balance Alert - Individual",
                TopupAmount = 500.00m,
                RuleTargetType = RuleTargetType.Individual,
                ScheduledTime = DateTime.UtcNow.AddHours(2),
                IsExecuted = false,
                Description = "Targeted financial assistance for individual account holder with critically low balance to support continued education access",
                InternalRemarks = "GIVING POOR PPL MONEY"
            },

            // 2. Batch Topup - Everyone
            new TopupRule
            {
                Id = "rule-002",
                RuleName = "Annual National Topup",
                TopupAmount = 1000.00m,
                RuleTargetType = RuleTargetType.Batch,
                BatchFilterType = BatchFilterType.Everyone,
                ScheduledTime = DateTime.UtcNow.AddDays(10),
                IsExecuted = false,
                Description = "Universal education fund distribution as part of national lifelong learning initiative. Applies to all eligible account holders regardless of status.",
                InternalRemarks = "National education funding policy 2026"
            },

            // 3. Batch Topup - Customized (Singapore Citizens in Primary School)
            new TopupRule
            {
                Id = "rule-003",
                RuleName = "Primary School Support - Citizen",
                TopupAmount = 300.00m,
                RuleTargetType = RuleTargetType.Batch,
                BatchFilterType = BatchFilterType.Customized,
                MinAge = 7,
                MaxAge = 12,
                ResidentialStatus = ResidentialStatus.SingaporeCitizen,
                ScheduledTime = DateTime.UtcNow.AddDays(5),
                IsExecuted = false,
                Description = "Supplementary funding for Singapore Citizen children aged 7-12 to support enrichment and supplementary learning programs during primary education years",
                InternalRemarks = "Early education support initiative"
            },

            // 4. Batch Topup - Customized (Low Balance Graduates)
            new TopupRule
            {
                Id = "rule-004",
                RuleName = "Graduate Bonus - Low Balance",
                TopupAmount = 750.00m,
                RuleTargetType = RuleTargetType.Batch,
                BatchFilterType = BatchFilterType.Customized,
                MinAge = 21,
                MaxBalance = 500.00m,
                ScheduledTime = DateTime.UtcNow.AddDays(14),
                IsExecuted = false,
                Description = "Financial assistance for adult learners aged 21+ with account balances below $500 to encourage continuous professional development and higher education pursuit",
                InternalRemarks = "Support for continuing education"
            }
        };

        await _context.TopupRules.AddRangeAsync(topupRules);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Seeded {Count} topup rules successfully", topupRules.Count);
    }

    private async Task SeedTopupRuleTargetsAsync()
    {
        _logger.LogInformation("Seeding topup rule targets...");

        // Only Individual topup rules need specific targets
        // rule-001 is an Individual topup for Tim's education account
        var topupRuleTargets = new List<TopupRuleTarget>
        {
            new TopupRuleTarget
            {
                Id = "target-001",
                TopupRuleId = "rule-001", // Low Balance Alert - Individual
                EducationAccountId = "cb50d51a-4b0f-42b7-9994-f06e3c15ea1c" // Tim's Education Account
            }
        };

        await _context.TopupRuleTargets.AddRangeAsync(topupRuleTargets);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} topup rule targets", topupRuleTargets.Count);
    }

    private async Task SeedBatchExecutionsAsync()
    {
        _logger.LogInformation("Seeding batch executions...");

        var batchExecutions = new List<BatchExecution>
        {
            new BatchExecution
            {
                Id = "batch-001",
                ScheduledTime = new DateTime(2026, 1, 1, 0, 0, 0),
                ExecutedTime = new DateTime(2026, 1, 1, 0, 5, 23),
                Status = TopUpStatus.Completed
            },
            new BatchExecution
            {
                Id = "batch-002",
                ScheduledTime = new DateTime(2026, 2, 1, 0, 0, 0),
                ExecutedTime = new DateTime(2026, 2, 1, 0, 4, 12),
                Status = TopUpStatus.Completed
            },
            new BatchExecution
            {
                Id = "batch-003",
                ScheduledTime = new DateTime(2026, 3, 1, 0, 0, 0),
                ExecutedTime = null,
                Status = TopUpStatus.Scheduled
            },
            // Future scheduled executions for testing
            new BatchExecution
            {
                Id = "batch-004",
                ScheduledTime = DateTime.UtcNow.AddHours(2),
                ExecutedTime = null,
                Status = TopUpStatus.Scheduled
            },
            new BatchExecution
            {
                Id = "batch-005",
                ScheduledTime = DateTime.UtcNow.AddDays(5),
                ExecutedTime = null,
                Status = TopUpStatus.Scheduled
            },
            new BatchExecution
            {
                Id = "batch-006",
                ScheduledTime = DateTime.UtcNow.AddDays(10),
                ExecutedTime = null,
                Status = TopUpStatus.Scheduled
            }
        };

        await _context.BatchExecutions.AddRangeAsync(batchExecutions);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} batch executions", batchExecutions.Count);
    }

    private async Task SeedBatchRuleExecutionsAsync()
    {
        _logger.LogInformation("Seeding batch rule executions...");

        var batchRuleExecutions = new List<BatchRuleExecution>
        {
            // Future scheduled INDIVIDUAL topup for rule-001
            new BatchRuleExecution
            {
                Id = "batchrule-001",
                BatchID = "batch-004",
                RuleID = "rule-001" // Low Balance Alert - Individual
            },
            // Future scheduled BATCH topup for rule-003
            new BatchRuleExecution
            {
                Id = "batchrule-002",
                BatchID = "batch-005",
                RuleID = "rule-003" // Primary School Support - Citizen
            },
            // Completed BATCH topup for rule-002
            new BatchRuleExecution
            {
                Id = "batchrule-003",
                BatchID = "batch-001",
                RuleID = "rule-002" // Annual National Topup (completed)
            },
            // Future scheduled BATCH topup for rule-004
            new BatchRuleExecution
            {
                Id = "batchrule-004",
                BatchID = "batch-006",
                RuleID = "rule-004" // Graduate Bonus - Low Balance
            }
        };

        await _context.BatchRuleExecutions.AddRangeAsync(batchRuleExecutions);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} batch rule executions", batchRuleExecutions.Count);
    }

    private async Task SeedTopupRuleAccountHoldersAsync()
    {
        _logger.LogInformation("Seeding topup rule account holders (completed top-ups)...");

        // Get some account holders for the completed batch top-up
        var accountHolders = await _context.AccountHolders
            .Take(4)
            .ToListAsync();

        if (!accountHolders.Any())
        {
            _logger.LogWarning("No account holders found to seed topup rule account holders");
            return;
        }

        var topupRuleAccountHolders = new List<TopupRuleAccountHolder>();
        
        // Simulate that batch-001 (Annual National Topup - rule-002) executed and affected some accounts
        var executedTime = new DateTime(2026, 1, 1, 0, 5, 23);
        foreach (var accountHolder in accountHolders)
        {
            var balanceBefore = 1000m + (accountHolders.IndexOf(accountHolder) * 500m);
            var topupAmount = 1000m;
            topupRuleAccountHolders.Add(new TopupRuleAccountHolder
            {
                TopupRuleId = "rule-002",
                AccountHolderId = accountHolder.Id,
                BalanceBefore = balanceBefore,
                BalanceAfter = balanceBefore + topupAmount,
                TopupAmount = topupAmount,
                ExecutedAt = executedTime
            });
        }

        await _context.TopupRuleAccountHolders.AddRangeAsync(topupRuleAccountHolders);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} topup rule account holder records", topupRuleAccountHolders.Count);
    }

    private async Task SeedSchoolingLevelsAsync()
    {
        _logger.LogInformation("Seeding schooling levels...");

        var schoolingLevels = new List<SchoolingLevel>
        {
            new SchoolingLevel { Id = "SL-001", Name = "Primary", Description = "Primary School Level" },
            new SchoolingLevel { Id = "SL-002", Name = "Secondary", Description = "Secondary School Level" },
            new SchoolingLevel { Id = "SL-003", Name = "PostSecondary", Description = "Post-Secondary Level (JC, Polytechnic, ITE)" },
            new SchoolingLevel { Id = "SL-004", Name = "Tertiary", Description = "University/Tertiary Education" },
            new SchoolingLevel { Id = "SL-005", Name = "PostGraduate", Description = "Masters / PhD level" }
        };

        await _context.SchoolingLevels.AddRangeAsync(schoolingLevels);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} schooling levels successfully", schoolingLevels.Count);
    }

    public async Task SeedProvidersAsync()
    {
        _logger.LogInformation("Seeding providers...");

        // Get schooling levels from database
        var primaryLevel = await _context.SchoolingLevels.FirstOrDefaultAsync(sl => sl.Name == "Primary");
        var secondaryLevel = await _context.SchoolingLevels.FirstOrDefaultAsync(sl => sl.Name == "Secondary");
        var postSecondaryLevel = await _context.SchoolingLevels.FirstOrDefaultAsync(sl => sl.Name == "PostSecondary");
        var tertiaryLevel = await _context.SchoolingLevels.FirstOrDefaultAsync(sl => sl.Name == "Tertiary");
        var postGraduateLevel = await _context.SchoolingLevels.FirstOrDefaultAsync(sl => sl.Name == "PostGraduate");

        var providers = ProviderSeedData.GetProvidersForSeeding(
            primaryLevel,
            secondaryLevel,
            postSecondaryLevel,
            tertiaryLevel,
            postGraduateLevel
        );

        await _context.Providers.AddRangeAsync(providers);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} providers successfully", providers.Count);
    }

    public async Task SeedResidentsAsync(int count = 50)
    {
        _logger.LogInformation("Seeding residents ({Count})...", count);

        var residents = ResidentSeedData.GetResidents(count);

        await _context.Set<Resident>().AddRangeAsync(residents);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} residents successfully", residents.Count);
    }

    public async Task<object> GetSeedStatusAsync()
    {
        var adminsCount = await _context.Admins.CountAsync();
        var accountHoldersCount = await _context.AccountHolders.CountAsync();
        var educationAccountsCount = await _context.EducationAccounts.CountAsync();
        var schoolingLevelsCount = await _context.SchoolingLevels.CountAsync();
        var providersCount = await _context.Providers.CountAsync();
        var coursesCount = await _context.Courses.CountAsync();
        var enrollmentsCount = await _context.Enrollments.CountAsync();
        var invoicesCount = await _context.Invoices.CountAsync();
        var transactionsCount = await _context.Transactions.CountAsync();
        var historyCount = await _context.HistoryOfChanges.CountAsync();
        var topupRulesCount = await _context.TopupRules.CountAsync();
        var topupRuleTargetsCount = await _context.TopupRuleTargets.CountAsync();
        var batchExecutionsCount = await _context.BatchExecutions.CountAsync();
        var batchRuleExecutionsCount = await _context.BatchRuleExecutions.CountAsync();
        var residentsCount = await _context.Set<Resident>().CountAsync();
        var educationLevelsCount = await _context.EducationLevels.CountAsync();
        var schoolingStatusesCount = await _context.SchoolingStatuses.CountAsync();

        return new
        {
            admins = new { count = adminsCount, isSeeded = adminsCount > 0 },
            accountHolders = new { count = accountHoldersCount, isSeeded = accountHoldersCount > 0 },
            educationAccounts = new { count = educationAccountsCount, isSeeded = educationAccountsCount > 0 },
            schoolingLevels = new { count = schoolingLevelsCount, isSeeded = schoolingLevelsCount > 0 },
            providers = new { count = providersCount, isSeeded = providersCount > 0 },
            courses = new { count = coursesCount, isSeeded = coursesCount > 0 },
            enrollments = new { count = enrollmentsCount, isSeeded = enrollmentsCount > 0 },
            invoices = new { count = invoicesCount, isSeeded = invoicesCount > 0 },
            transactions = new { count = transactionsCount, isSeeded = transactionsCount > 0 },
            historyOfChanges = new { count = historyCount, isSeeded = historyCount > 0 },
            topupRules = new { count = topupRulesCount, isSeeded = topupRulesCount > 0 },
            topupRuleTargets = new { count = topupRuleTargetsCount, isSeeded = topupRuleTargetsCount > 0 },
            batchExecutions = new { count = batchExecutionsCount, isSeeded = batchExecutionsCount > 0 },
            batchRuleExecutions = new { count = batchRuleExecutionsCount, isSeeded = batchRuleExecutionsCount > 0 },
            residents = new { count = residentsCount, isSeeded = residentsCount > 0 },
            educationLevels = new { count = educationLevelsCount, isSeeded = educationLevelsCount > 0 },
            schoolingStatuses = new { count = schoolingStatusesCount, isSeeded = schoolingStatusesCount > 0}
        };
    }

    private async Task SeedEducationLevelsAsync()
    {
        _logger.LogInformation("Seeding education levels...");

        if (await _context.EducationLevels.AnyAsync()) return;

        var educationLevels = new List<EducationLevelDefinition>
        {
            new EducationLevelDefinition
            {
                Id = "EL-001",
                Name = "Primary",
                Description = "Primary education level"
            },
            new EducationLevelDefinition
            {
                Id = "EL-002",
                Name = "Secondary",
                Description = "Secondary education level"
            },
            new EducationLevelDefinition
            {
                Id = "EL-003",
                Name = "PostSecondary",
                Description = "Post-secondary education (ITE, Poly, JC)"
            },
            new EducationLevelDefinition
            {
                Id = "EL-004",
                Name = "Tertiary",
                Description = "University / Degree level"
            },
            new EducationLevelDefinition
            {
                Id = "EL-005",
                Name = "PostGraduate",
                Description = "Masters / PhD level"
            },
            new EducationLevelDefinition
            {
                Id = "EL-006",
                Name = "NotSet",
                Description = "Not set education level"
            },
        };

        await _context.EducationLevels.AddRangeAsync(educationLevels);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} education levels", educationLevels.Count);
    }

    private async Task SeedSchoolingStatusesAsync()
    {
        _logger.LogInformation("Seeding schooling statuses...");

        if (await _context.SchoolingStatuses.AnyAsync())
        {
            _context.SchoolingStatuses.RemoveRange(_context.SchoolingStatuses);
            await _context.SaveChangesAsync();
        }

        var schoolingStatuses = new List<SchoolingStatusDefinition>
        {
            new SchoolingStatusDefinition
            {
                Id = "SS-001",
                Name = "InSchool",
                Description = "Currently enrolled in school"
            },
            new SchoolingStatusDefinition
            {
                Id = "SS-002",
                Name = "NotInSchool",
                Description = "Not currently enrolled in school"
            },
        };

        await _context.SchoolingStatuses.AddRangeAsync(schoolingStatuses);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} schooling statuses", schoolingStatuses.Count);
    }

}
