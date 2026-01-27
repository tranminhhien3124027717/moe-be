using MOE_System.EService.Domain.Enums;

namespace MOE_System.EService.Domain.Entities;

public class Invoice
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EnrollmentID { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentType PaymentType { get; set; }
    public string? BillingCycle { get; set; }
    public DateTime? BillingPeriodStart { get; set; }
    public DateTime? BillingPeriodEnd { get; set; }
    public DateTime BillingDate { get; set; }
    public int? PaymentDue { get; set; }
    public DateTime? DueDate { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Outstanding;

    // Navigation properties
    public Enrollment? Enrollment { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
