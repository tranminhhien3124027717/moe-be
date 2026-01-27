using MOE_System.EService.Application.DTOs;
using MOE_System.EService.Application.DTOs.EducationAccount;

namespace MOE_System.EService.Application.Interfaces.Services
{
    public interface IEducationAccountService
    {
        Task<OutstandingFeeResponse> GetOutstandingFeeAsync(string educationAccountId);
        Task<TransactionDetailResponse> GetTransactionDetailAsync(string accountId, string transactionId);
    }
}
