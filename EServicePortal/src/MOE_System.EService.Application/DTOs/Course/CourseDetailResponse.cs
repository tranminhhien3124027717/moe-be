namespace MOE_System.EService.Application.DTOs.Course;

public sealed record CourseDetailResponse
{
    public CourseInformation Course { get; init; } = null!;
    public PaymentSummaryInfo PaymentSummary { get; init; } = null!;
    public List<PaymentHistoryDetail> PaymentHistory { get; init; } = new();
}

public sealed record CourseInformation
{
    public string CourseId { get; init; } = string.Empty;
    public string CourseName { get; init; } = string.Empty;
    public string ProviderName { get; init; } = string.Empty;
    public DateTime CourseStart { get; init; }
    public DateTime? CourseEnd { get; init; }
    public string PaymentType { get; init; } = string.Empty;
    public string? BillingCycle { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal TotalFee { get; init; }
}

public sealed record PaymentSummaryInfo
{
    public decimal TotalCharged { get; init; }
    public decimal TotalPaid { get; init; }
    public decimal Outstanding { get; init; }
}

public sealed record PaymentHistoryDetail
{
    public string InvoiceId { get; init; } = string.Empty;
    public DateTime PaymentDate { get; init; }
    public string CourseName { get; init; } = string.Empty;
    public string PaidCycle { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}
