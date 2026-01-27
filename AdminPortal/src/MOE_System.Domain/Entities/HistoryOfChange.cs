using MOE_System.Domain.Enums;

namespace MOE_System.Domain.Entities;

public class HistoryOfChange
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EducationAccountId { get; set; } = string.Empty;
    public string ReferenceId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public ChangeType Type { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }

    // Navigation property
    public EducationAccount? EducationAccount { get; set; }
}
