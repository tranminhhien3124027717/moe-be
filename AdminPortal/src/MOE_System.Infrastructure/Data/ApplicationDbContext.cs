using Microsoft.EntityFrameworkCore;
using MOE_System.Domain.Entities;

namespace MOE_System.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets for all entities
    public DbSet<Admin> Admins { get; set; }
    public DbSet<AccountHolder> AccountHolders { get; set; }
    public DbSet<EducationAccount> EducationAccounts { get; set; }
    public DbSet<HistoryOfChange> HistoryOfChanges { get; set; }
    public DbSet<TopupRule> TopupRules { get; set; }
    public DbSet<TopupRuleTarget> TopupRuleTargets { get; set; }
    public DbSet<TopupExecutionSnapshot> TopupExecutionSnapshots { get; set; }
    public DbSet<BatchExecution> BatchExecutions { get; set; }
    public DbSet<BatchRuleExecution> BatchRuleExecutions { get; set; }
    public DbSet<Provider> Providers { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Enrollment> Enrollments { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Resident> Residents { get; set; }
    public DbSet<SchoolingLevel> SchoolingLevels { get; set; }
    public DbSet<TopupRuleAccountHolder> TopupRuleAccountHolders { get; set; }
    public DbSet<EducationLevelDefinition> EducationLevels { get; set; }
    public DbSet<SchoolingStatusDefinition> SchoolingStatuses { get; set; }
    public DbSet<TopUpConfig> TopUpConfigs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Resident>(entity =>
        {
            entity.HasKey(e => e.NRIC);
        });

        modelBuilder.Entity<TopUpConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RuleName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.TopupAmount).HasPrecision(18, 2);
            entity.Property(e => e.MinAge).IsRequired(false);
            entity.Property(e => e.MaxAge).IsRequired(false);
            entity.Property(e => e.MinBalance).HasPrecision(18, 2).IsRequired(false);
            entity.Property(e => e.MaxBalance).HasPrecision(18, 2).IsRequired(false);
        });
        // Configure Admin
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Password).IsRequired().HasMaxLength(256);
        });

        // Configure AccountHolder and EducationAccount (1-to-1)
        modelBuilder.Entity<AccountHolder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.NRIC).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CitizenId).IsRequired().HasMaxLength(50);

            entity.HasOne(e => e.EducationAccount)
                .WithOne(e => e.AccountHolder)
                .HasForeignKey<EducationAccount>(e => e.AccountHolderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.NRIC).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Configure EducationAccount
        modelBuilder.Entity<EducationAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Password).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Balance).HasPrecision(18, 2);
        });

        // Configure HistoryOfChange
        modelBuilder.Entity<HistoryOfChange>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.BalanceBefore).HasPrecision(18, 2);
            entity.Property(e => e.BalanceAfter).HasPrecision(18, 2);
            entity.Property(e => e.ReferenceId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Type).IsRequired();

            entity.HasOne(e => e.EducationAccount)
                .WithMany(e => e.HistoryOfChanges)
                .HasForeignKey(e => e.EducationAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure TopupRule
        modelBuilder.Entity<TopupRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RuleName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.TopupAmount).HasPrecision(18, 2);

            entity.Property(e => e.RuleTargetType).HasConversion<string>().IsRequired().HasMaxLength(50);
            entity.Property(e => e.BatchFilterType).HasConversion<string>().HasMaxLength(50);

            entity.Property(e => e.MinAge).IsRequired(false);
            entity.Property(e => e.MaxAge).IsRequired(false);
            entity.Property(e => e.MinBalance).HasPrecision(18, 2).IsRequired(false);
            entity.Property(e => e.MaxBalance).HasPrecision(18, 2).IsRequired(false);

            entity.Property(e => e.ResidentialStatus).HasConversion<string>().HasMaxLength(100);

            entity.Property(e => e.ScheduledTime).IsRequired();
            entity.Property(e => e.IsExecuted).HasDefaultValue(false);

            // Many-to-many navigations will be handled implicitly by EF Core.
                    }
        );

        modelBuilder.Entity<EducationLevelDefinition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<SchoolingStatusDefinition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
        });


        modelBuilder.Entity<TopupRuleTarget>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.TopupRuleId, e.EducationAccountId })
                  .IsUnique();

            entity.Property(e => e.IsActive).HasDefaultValue(true);


            entity.HasOne(e => e.TopupRule)
                .WithMany(r => r.Targets)
                .HasForeignKey(e => e.TopupRuleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.EducationAccount)
                .WithMany(a => a.TopupRuleTargets)
                .HasForeignKey(e => e.EducationAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TopupExecutionSnapshot>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.BalanceBefore).HasPrecision(18, 2);
            entity.Property(e => e.BalanceAfter).HasPrecision(18, 2);

            entity.HasIndex(e => new { e.TopupRuleId, e.EducationAccountId })
                  .IsUnique();

            entity.HasOne(e => e.TopupRule)
                .WithMany(r => r.TopupExecutionSnapshots)
                .HasForeignKey(e => e.TopupRuleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.EducationAccount)
                .WithMany(a => a.TopupExecutionSnapshots)
                .HasForeignKey(e => e.EducationAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure BatchExecution
        modelBuilder.Entity<BatchExecution>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
        });

        // Configure BatchRuleExecution (junction table)
        modelBuilder.Entity<BatchRuleExecution>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.BatchExecution)
                .WithMany(e => e.BatchRuleExecutions)
                .HasForeignKey(e => e.BatchID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.TopupRule)
                .WithMany(e => e.BatchRuleExecutions)
                .HasForeignKey(e => e.RuleID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Provider
        modelBuilder.Entity<Provider>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });

        // Configure Course
        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CourseName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CourseCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FeeAmount).HasPrecision(18, 2);
            entity.Property(e => e.FeePerCycle).HasPrecision(18, 2);
            entity.Property(e => e.PaymentType).IsRequired();
            entity.Property(e => e.TermName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.BillingCycle).HasMaxLength(50).IsRequired(false);

            entity.Property(e => e.BillingDate).IsRequired(false);

            entity.Property(e => e.PaymentDue).IsRequired(false);

            entity.HasOne(e => e.Provider)
                .WithMany(e => e.Courses)
                .HasForeignKey(e => e.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Enrollment
        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);

            entity.HasOne(e => e.Course)
                .WithMany(e => e.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.EducationAccount)
                .WithMany(e => e.Enrollments)
                .HasForeignKey(e => e.EducationAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Invoice
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PaymentType).IsRequired();
            entity.Property(e => e.BillingCycle).HasMaxLength(50).IsRequired(false);

            entity.Property(e => e.PaymentDue).IsRequired(false);

            entity.Property(e => e.BillingDate)
                .IsRequired();

            entity.Property(e => e.BillingPeriodStart)
                .IsRequired();

            entity.Property(e => e.BillingPeriodEnd)
                .IsRequired();

            entity.Property(e => e.DueDate)
                .IsRequired(false);

            entity.Property(e => e.PaymentDue)
                .IsRequired(false);

            entity.HasOne(e => e.Enrollment)
                .WithMany(e => e.Invoices)
                .HasForeignKey(e => e.EnrollmentID)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(nameof(Invoice.EnrollmentID), nameof(Invoice.BillingPeriodStart)).IsUnique();
        });

        // Configure Transaction
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.PaymentMethod).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);

            entity.HasOne(e => e.Invoice)
                .WithMany(e => e.Transactions)
                .HasForeignKey(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure SchoolingLevel
        modelBuilder.Entity<SchoolingLevel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        // Configure M:M relationship between Provider and SchoolingLevel
        modelBuilder.Entity<Provider>()
            .HasMany(p => p.SchoolingLevels)
            .WithMany(sl => sl.Providers)
            .UsingEntity<Dictionary<string, object>>(
                "ProviderSchoolingLevel",
                j => j.HasOne<SchoolingLevel>().WithMany().HasForeignKey("SchoolingLevelId"),
                j => j.HasOne<Provider>().WithMany().HasForeignKey("ProviderId"));

        // Configure TopupRuleAccountHolder (junction table)
        modelBuilder.Entity<TopupRuleAccountHolder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BalanceBefore).HasPrecision(18, 2);
            entity.Property(e => e.BalanceAfter).HasPrecision(18, 2);
            entity.Property(e => e.TopupAmount).HasPrecision(18, 2);
            entity.Property(e => e.ExecutedAt).IsRequired();

            entity.HasOne(e => e.TopupRule)
                .WithMany(e => e.TopupRuleAccountHolders)
                .HasForeignKey(e => e.TopupRuleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AccountHolder)
                .WithMany(e => e.TopupRuleAccountHolders)
                .HasForeignKey(e => e.AccountHolderId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        modelBuilder.Entity<OutBoxMessage>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Type).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Content).IsRequired();

            entity.HasIndex(e => new { e.ProcessedOn, e.OccurredOn })
                      .HasDatabaseName("IX_OutboxMessages_Queue");
        });

    }
}
