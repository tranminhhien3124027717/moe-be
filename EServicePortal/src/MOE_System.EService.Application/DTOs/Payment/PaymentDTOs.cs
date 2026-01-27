using MOE_System.EService.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace MOE_System.EService.Application.DTOs.Payment
{
    #region API Request/Response DTOs
    
    public class CreatePaymentRequest
    {
        [Required(ErrorMessage = "InvoiceId is required")]
        public required string InvoiceId { get; set; }

        public bool IsUseBalance { get; set; }
        
        public decimal? AmountFromBalance { get; set; }
        
        public bool IsUseExternal { get; set; }
        
        public PaymentMethod? PaymentMethod { get; set; }
    }
   

    public class CreateCardPaymentResponse
    {
        public required string InvoiceId { get; set; }
        public required string TransactionId { get; set; }
        public required string PaymentIntentId { get; set; }
        public required string ClientSecret { get; set; }
        public decimal Amount { get; set; }
        public required string Currency { get; set; }
        public required string Status { get; set; }
        public DateTime ExpiresAt { get; set; }
    }


    public class CreateBankTransferPaymentResponse
    {
        public required string InvoiceId { get; set; }
        public required string TransactionId { get; set; }
        public required string PaymentIntentId { get; set; }
        public decimal Amount { get; set; }
        public DateTime ExpiresAt { get; set; }
        public required string QRCodeUrl { get; set; }
        public required string QRCodeData { get; set; }
        public required string HostedInstructionsUrl { get; set; }
    }

    public class CreateCombinedPaymentResponse
    {
        public required string InvoiceId { get; set; }
        public required string BalanceTransactionId { get; set; }
        public decimal AmountFromBalance { get; set; }
        public decimal BalanceAfter { get; set; }
        
        public required string ExternalTransactionId { get; set; }
        public decimal AmountFromExternal { get; set; }
        public required string PaymentMethod { get; set; }
        
        // Card payment specific
        public string? PaymentIntentId { get; set; }
        public string? ClientSecret { get; set; }
        public string? Currency { get; set; }
        
        // Bank transfer specific
        public string? QRCodeUrl { get; set; }
        public string? QRCodeData { get; set; }
        public string? HostedInstructionsUrl { get; set; }
        public string? ReferenceNumber { get; set; }
        
        public DateTime? ExpiresAt { get; set; }
        public required string Status { get; set; }
    }

    public class ProcessPaymentRequest
    {
        public required string TransactionId { get; set; }
        public required string InvoiceId { get; set; }
        public string? PaymentIntentId { get; set; }
    }

    public class ProcessPaymentResponse
    {
        public required string InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public required string Status { get; set; }
        public DateTime TransactionAt { get; set; }
    }

    public class TransactionResponse
    {
        public required string Id { get; set; }
        public decimal Amount { get; set; }
        public required string InvoiceId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? TransactionAt { get; set; }
        public required string PaymentMethod { get; set; }
        public required string Status { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? CourseName { get; set; }
    }

    public class PaymentSummaryResponse
    {
        public decimal TotalPaid { get; set; }
        public decimal TotalOutstanding { get; set; }
        public int TotalTransactions { get; set; }
        public int OutstandingInvoices { get; set; }
    }

    #endregion

    #region Cache DTOs

    public class CombinedPaymentCacheData
    {
        public required string BalanceTransactionId { get; set; }
        public required string ExternalTransactionId { get; set; }
        public decimal AmountFromBalance { get; set; }
        public required string InvoiceId { get; set; }
        public required string AccountHolderId { get; set; }
    }

    public class CachedPaymentResponse
    {
        public required string TransactionId { get; set; }
        public required string PaymentIntentId { get; set; }
        public required string InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public DateTime ExpiresAt { get; set; }
        public required string PaymentType { get; set; }
        
        // Card payment specific
        public string? ClientSecret { get; set; }
        public string? Currency { get; set; }
        public string? Status { get; set; }
        
        // Bank transfer specific
        public string? QRCodeData { get; set; }
        public string? QRCodeUrl { get; set; }
        public string? HostedInstructionsUrl { get; set; }
        public string? ReferenceNumber { get; set; }
    }

    #endregion

    #region Stripe DTOs

    public class StripePaymentRequest
    {
        public required decimal Amount { get; set; }
        public required StripePaymentMetadata Metadata { get; set; }
    }

    public class StripePaymentMetadata
    {
        public string TransactionId { get; set; } = string.Empty;
        public required string InvoiceNumber { get; set; }
        public required string Email { get; set; }
        public required string EducationAccountId { get; set; }
    }

    public class StripeCardPaymentResult
    {
        public required string PaymentIntentId { get; set; }
        public required string ClientSecret { get; set; }
        public required string Currency { get; set; }
        public required string Status { get; set; }
    }

    public class StripePayNowPaymentResult
    {
        public required string PaymentIntentId { get; set; }
        public required string QRCodeUrl { get; set; }
        public required string QRCodeData { get; set; }
        public required string HostedInstructionsUrl { get; set; }
        public required string Status { get; set; }
    }

    #endregion
}
