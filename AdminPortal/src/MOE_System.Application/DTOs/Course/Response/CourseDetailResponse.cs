namespace MOE_System.Application.DTOs.Course.Response;

public sealed record CourseDetailResponse
(
    string CourseId,
    string CourseCode,
    string CourseName,
    string ProviderId,
    string ProviderName,
    string EducationLevel,
    string ModeOfTraining,
    string Status,
    DateTime StartDate,
    DateTime EndDate,
    string PaymentType,
    string? BillingCycle,
    decimal? FeePerCycle,
    decimal TotalFee,
    IReadOnlyList<EnrolledStudent> EnrolledStudents,
    int? BillingDate,
    int? PaymentDue
);

public sealed record EnrolledStudent
(
    string AccountHolderId,
    string EducationAccountId,
    string StudentName,
    string NRIC,
    decimal TotalPaid,
    decimal TotalDue,
    DateTime EnrolledAt
);