using MOE_System.EService.Application.DTOs.Payment;

namespace MOE_System.EService.Application.Interfaces
{
    public interface IStripeService
    {
        Task<StripeCardPaymentResult> CreateCardPaymentIntentAsync(StripePaymentRequest request);
        Task<StripePayNowPaymentResult> CreatePayNowPaymentIntentAsync(StripePaymentRequest request);
    }
}
