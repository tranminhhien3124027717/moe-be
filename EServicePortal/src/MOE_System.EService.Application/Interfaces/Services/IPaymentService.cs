using MOE_System.EService.Application.Common;
using MOE_System.EService.Application.DTOs.Payment;
using MOE_System.EService.Application.DTOs.PaymentByCreditCard.Request;
using MOE_System.EService.Application.DTOs.PaymentByCreditCard.Response;

namespace MOE_System.EService.Application.Interfaces.Services
{
    public interface IPaymentService
    {
        // Unified payment creation endpoint
        Task<object> CreatePaymentAsync(string accountHolderId, CreatePaymentRequest request);
        // Specific payment method handlers
        Task<object> CreateBalancePaymentAsync(string accountHolderId, string invoiceId);
        Task<object> CreateCombinedPaymentAsync(string accountHolderId, CreatePaymentRequest request);
        Task<ProcessPaymentResponse> ProcessPaymentInternalAsync(ProcessPaymentRequest request);
        Task<bool> CancelPaymentInternalAsync(string transactionId);
        Task<int> TimeoutExpiredPaymentsAsync();
        Task<TransactionResponse> GetTransactionByIdAsync(string transactionId);
        Task<PaginatedList<TransactionResponse>> GetTransactionHistoryAsync(string accountHolderId, int pageIndex, int pageSize);
        Task<PaymentSummaryResponse> GetPaymentSummaryAsync(string accountHolderId);
        Task<PaymentByCreditCardResponse> PayByCreditCardAsync(Guid educationAccountId, PaymentByCreditCardRequest request, CancellationToken cancellationToken);
        Task<InvoiceDetailsResponse> GetInvoiceDetails(string invoiceId, string accountHolderId);
        Task<int> CheckInvoiceStatus(string invoiceId);
    }
}

