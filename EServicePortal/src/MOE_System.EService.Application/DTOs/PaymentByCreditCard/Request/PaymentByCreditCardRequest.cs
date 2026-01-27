namespace MOE_System.EService.Application.DTOs.PaymentByCreditCard.Request;

public class PaymentByCreditCardRequest
{
    public string InvoiceId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string CardHolderName { get; set; } = string.Empty;
    public string ExpiryMonth { get; set; } = string.Empty; // MM
    public string ExpiryYear { get; set; } = string.Empty; // YY or YYYY
    public string CVV { get; set; } = string.Empty;
    public string? BillingAddress { get; set; }
    public string? PostalCode { get; set; }
}
