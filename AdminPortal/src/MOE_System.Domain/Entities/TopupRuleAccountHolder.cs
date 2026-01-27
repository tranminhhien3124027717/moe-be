namespace MOE_System.Domain.Entities;

public class TopupRuleAccountHolder
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TopupRuleId { get; set; } = string.Empty;
    public string AccountHolderId { get; set; } = string.Empty;
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public decimal TopupAmount { get; set; }
    public DateTime ExecutedAt { get; set; }

    // Navigation properties
    public TopupRule? TopupRule { get; set; }
    
    public AccountHolder? AccountHolder { get; set; }
}
