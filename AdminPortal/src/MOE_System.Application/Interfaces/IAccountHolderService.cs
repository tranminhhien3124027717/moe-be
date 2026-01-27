using MOE_System.Application.DTOs;
using MOE_System.Application.Common;
using MOE_System.Application.DTOs.AccountHolder;
using MOE_System.Application.DTOs.AccountHolder.Request;
using MOE_System.Application.DTOs.AccountHolder.Response;

namespace MOE_System.Application.Interfaces;

public interface IAccountHolderService
{
    Task<PaginatedList<AccountHolderResponse>> GetAccountHoldersAsync(int pageNumber = 1, int pageSize = 20, AccountHolderFilterParams? filters = null);
    Task<AccountHolderDetailResponse> GetAccountHolderDetailAsync(string accountHolderId);
    Task<PaginatedList<EnrolledCourseInfo>> GetEnrolledCoursesAsync(string accountHolderId, int pageNumber = 1, int pageSize = 5);
    Task<PaginatedList<OutstandingFeeInfo>> GetOutstandingFeesAsync(string accountHolderId, int pageNumber = 1, int pageSize = 5);
    Task<PaginatedList<TopUpHistoryInfo>> GetTopUpHistoryAsync(string accountHolderId, int pageNumber = 1, int pageSize = 5);
    Task<PaginatedList<PaymentHistoryInfo>> GetPaymentHistoryAsync(string accountHolderId, int pageNumber = 1, int pageSize = 5);
    Task<StudentCourseDetailResponse> GetStudentCourseDetailAsync(string accountHolderId, string courseId);
    Task<ResidentInfoResponse> GetResidentAccountHolderByNRICAsync(string nric);
    Task<AccountHolderResponse> AddAccountHolderAsync(CreateAccountHolderRequest request);
    Task UpdateAccountHolderAsync(EditAccountHolderRequest  request);
    Task ActivateAccountAsync(string accountHolderId);
    Task DeactivateAccountAsync(string accountHolderId);
    Task<BulkAccountOperationResponse> ActivateAccountsAsync(BulkAccountOperationRequest request);
    Task<BulkAccountOperationResponse> DeactivateAccountsAsync(BulkAccountOperationRequest request);
}
