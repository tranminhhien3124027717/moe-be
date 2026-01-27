using Microsoft.AspNetCore.Mvc;
using MOE_System.Infrastructure.Data.Seeding;
using MOE_System.Infrastructure.Data;

namespace MOE_System.API.Controllers;

[ApiController]
[Route("api/seeder")]
public class SeedController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseSeeder> _seederLogger;
    private readonly ILogger<SeedController> _logger;

    public SeedController(
        ApplicationDbContext context,
        ILogger<DatabaseSeeder> seederLogger,
        ILogger<SeedController> logger)
    {
        _context = context;
        _seederLogger = seederLogger;
        _logger = logger;
    }

    /// <summary>
    /// Seed all tables with comprehensive test data
    /// </summary>
    [HttpPost("all")]
    public async Task<IActionResult> SeedAll()
    {
        try
        {
            var seeder = new DatabaseSeeder(_context, _seederLogger);
            await seeder.SeedAsync();
            
            var status = await seeder.GetSeedStatusAsync();
            
            return Ok(new 
            { 
                message = "Database seeded successfully", 
                data = status 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding database");
            return StatusCode(500, new { message = "Error seeding database", error = ex.Message });
        }
    }

    /// <summary>
    /// Get the current status of seeded data for all tables
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetSeedStatus()
    {
        try
        {
            var seeder = new DatabaseSeeder(_context, _seederLogger);
            var status = await seeder.GetSeedStatusAsync();
            
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting seed status");
            return StatusCode(500, new { message = "Error getting seed status", error = ex.Message });
        }
    }

    [HttpPost("providers")]
    public async Task<IActionResult> SeedProviders()
    {
        try
        {
            var seeder = new DatabaseSeeder(_context, _seederLogger);
            await seeder.SeedProvidersAsync();
            
            return Ok(new { message = "Providers seeded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding providers");
            return StatusCode(500, new { message = "Error seeding providers", error = ex.Message });
        }
    }

    [HttpPost("residents")]
    public async Task<IActionResult> SeedResidents(int count = 50)
    {
        try
        {
            var seeder = new DatabaseSeeder(_context, _seederLogger);
            await seeder.SeedResidentsAsync(count);
            
            return Ok(new { message = "Residents seeded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding residents");
            return StatusCode(500, new { message = "Error seeding residents", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete all data for specific NRICs (AccountHolder, EducationAccount, and all related data)
    /// </summary>
    [HttpDelete("accounts/clear-test-accounts")]
    public async Task<IActionResult> DeleteTestAccounts()
    {
        // List of NRICs to delete
        var nricsToDelete = new List<string>
        {
            "T0375353G", "G0360687U", "F9890160N", "T1005124A", "S9607567E", "F9716779Q"
        };

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Find AccountHolders by NRIC
            var holders = _context.AccountHolders.Where(a => nricsToDelete.Contains(a.NRIC)).ToList();
            var holderIds = holders.Select(h => h.Id).ToList();

            // Find EducationAccounts by AccountHolderId
            var eduAccounts = _context.EducationAccounts.Where(ea => holderIds.Contains(ea.AccountHolderId)).ToList();
            var eduAccountIds = eduAccounts.Select(ea => ea.Id).ToList();

            // Delete related data (FKs)
            // Enrollments
            var enrollments = _context.Enrollments.Where(e => eduAccountIds.Contains(e.EducationAccountId)).ToList();
            var enrollmentIds = enrollments.Select(e => e.Id).ToList();
            _context.Enrollments.RemoveRange(enrollments);

            // Invoices
            var invoices = _context.Invoices.Where(i => enrollmentIds.Contains(i.EnrollmentID)).ToList();
            var invoiceIds = invoices.Select(i => i.Id).ToList();
            _context.Invoices.RemoveRange(invoices);

            // Transactions
            var transactions = _context.Transactions.Where(t => invoiceIds.Contains(t.InvoiceId)).ToList();
            _context.Transactions.RemoveRange(transactions);

            // HistoryOfChanges
            var histories = _context.HistoryOfChanges.Where(h => eduAccountIds.Contains(h.EducationAccountId)).ToList();
            _context.HistoryOfChanges.RemoveRange(histories);

            // TopupRuleTargets
            var topupRuleTargets = _context.TopupRuleTargets.Where(t => eduAccountIds.Contains(t.EducationAccountId)).ToList();
            _context.TopupRuleTargets.RemoveRange(topupRuleTargets);

            // Remove EducationAccounts
            _context.EducationAccounts.RemoveRange(eduAccounts);

            // Remove AccountHolders
            _context.AccountHolders.RemoveRange(holders);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { message = "Test accounts and related data deleted successfully." });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error deleting test accounts");
            return StatusCode(500, new { message = "Error deleting test accounts", error = ex.Message });
        }
    }
}
