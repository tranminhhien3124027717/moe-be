namespace MOE_System.Application.DTOs.AccountHolder.Response;

public sealed record StudentCourseDetailResponse
(
    AccountHolderInfo AccountHolder,
    CourseInfo Course,
    EnrollmentInfo Enrollment,
    PaymentSummary PaymentSummary,
    IReadOnlyList<OutstandingFeeItem> OutstandingFees,
    IReadOnlyList<PaymentHistoryItem> PaymentHistory
);

public sealed record AccountHolderInfo
(
    string Id,
    string FullName,
    string NRIC,
    string Email,
    string PhoneNumber,
    int TotalEnrolledCourses
);

public sealed record CourseInfo
(
    string Id,
    string CourseCode,
    string CourseName,
    string ProviderName,
    string? EducationLevel,
    string ModeOfTraining,
    string Status,
    DateTime StartDate,
    DateTime? EndDate,
    string PaymentType,
    string? BillingCycle,
    decimal FeePerCycle,
    decimal TotalFee,
    int? BillingDate,
    int? PaymentDue
);

public sealed record EnrollmentInfo
(
    string Id,
    DateTime EnrollmentDate,
    string Status
);

public sealed record PaymentSummary
(
    decimal TotalCharged,
    decimal TotalPaid,
    decimal Outstanding
);

public sealed record OutstandingFeeItem
(
    string Id,
    string BillingCycle,
    DateTime? BillingDate,
    DateTime DueDate,
    decimal Amount,
    decimal Paid,
    string Status
);

public sealed record PaymentHistoryItem
(
    string Id,
    DateTime PaymentDate,
    string CourseName,
    string PaidCycle,
    decimal Amount,
    string PaymentMethod,
    string Status
);
