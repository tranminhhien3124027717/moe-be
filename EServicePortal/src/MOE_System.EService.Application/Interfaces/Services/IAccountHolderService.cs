using MOE_System.EService.Application.DTOs;
using MOE_System.EService.Application.DTOs.AccountHolder;
using MOE_System.EService.Application.DTOs.Course;
using MOE_System.EService.Application.DTOs.EducationAccount;

namespace MOE_System.EService.Application.Interfaces.Services
{
    public interface IAccountHolderService
    {
        Task<AccountHolderResponse> GetAccountHolderAsync(string accountHolderId);
        Task<AccountHolderProfileResponse> GetMyProfileAsync(string accountHolderId);
        Task<UpdateProfileResponse> UpdateProfileAsync(string accountHolderId, UpdateProfileRequest request);
        Task<CourseDetailResponse> GetCourseDetailAsync(string accountHolderId, string enrollmentId);
        Task SyncProfileAsync(string accountHolderId);
        
        // Paginated course endpoints
        Task<CourseSummaryResponse> GetCourseSummaryAsync(string accountHolderId);
        Task<EnrolledCoursesResponse> GetEnrolledCoursesAsync(string accountHolderId, EnrolledCoursesRequest request);
        Task<PendingFeesResponse> GetPendingFeesAsync(string accountHolderId, PendingFeesRequest request);
        Task<PaymentHistoryResponse> GetPaymentHistoryAsync(string accountHolderId, PaymentHistoryRequest request);

        // Course details by enrollment
        Task<CourseInformationResponse> GetCourseInformationAsync(string accountHolderId, string enrollmentId);
        Task<OutstandingFeesResponse> GetOutstandingFeesAsync(string accountHolderId, string enrollmentId, OutstandingFeesRequest request);
        Task<UpcomingBillingCyclesResponse> GetUpcomingBillingCyclesAsync(string accountHolderId, string enrollmentId, UpcomingBillingCyclesRequest request);
        Task<EnrollmentPaymentHistoryResponse> GetEnrollmentPaymentHistoryAsync(string accountHolderId, string enrollmentId, EnrollmentPaymentHistoryRequest request);

        // Account balance transaction history
        Task<BalanceResponse> GetBalanceAsync(string accountHolderId);
        Task<BalanceHistoryResponse> GetTransactionHistoryAsync(string accountHolderId, BalanceHistoryRequest request);
    }
}
