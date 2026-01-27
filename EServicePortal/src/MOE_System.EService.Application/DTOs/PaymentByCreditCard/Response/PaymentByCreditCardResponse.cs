namespace MOE_System.EService.Application.DTOs.PaymentByCreditCard.Response;

public class PaymentByCreditCardResponse
{
    public string TransactionId { get; set; } = string.Empty;
    public string InvoiceId { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string LastFourDigits { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
