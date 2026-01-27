namespace MOE_System.EService.Domain.Entities;

public sealed class TopupExecutionSnapshot
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // Foreign keys
    public string TopupRuleId { get; set; } = string.Empty;
    public string EducationAccountId { get; set; } = string.Empty;

    // Snapshot data
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public DateTime ExecutedAt { get; set; }

    // Navigation
    public TopupRule TopupRule { get; set; } = null!;
    public EducationAccount EducationAccount { get; set; } = null!;
}
