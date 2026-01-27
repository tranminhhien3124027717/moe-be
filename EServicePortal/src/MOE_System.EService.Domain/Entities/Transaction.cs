using MOE_System.EService.Domain.Common;
using MOE_System.EService.Domain.Enums;

namespace MOE_System.EService.Domain.Entities;

public class Transaction : BaseEntity
{
    public decimal Amount { get; set; }
    public string InvoiceId { get; set; } = string.Empty;
    public DateTime? TransactionAt { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.AccountBalance;
    public TransactionStatus Status { get; set; } = TransactionStatus.Hold;

    public string? PaymentIntentId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Description { get; set; }

    // Navigation property
    public Invoice? Invoice { get; set; }
}
