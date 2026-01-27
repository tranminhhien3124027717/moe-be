using MOE_System.EService.Application.DTOs.Payment;
using MOE_System.EService.Application.Interfaces;
using MOE_System.EService.Application.Settings;
using Microsoft.Extensions.Options;
using Stripe;

namespace MOE_System.EService.Infrastructure.Services
{
    public class StripeService : IStripeService
    {
        private readonly StripeSettings _stripeSettings;

        public StripeService(IOptions<StripeSettings> stripeSettings)
        {
            _stripeSettings = stripeSettings.Value;
            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
        }

        public async Task<StripeCardPaymentResult> CreateCardPaymentIntentAsync(StripePaymentRequest request)
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(request.Amount * 100),
                Currency = "sgd",
                PaymentMethodTypes = new List<string> { "card" },
                Metadata = new Dictionary<string, string>
                {
                    { "TransactionId", request.Metadata.TransactionId },
                    { "InvoiceNumber", request.Metadata.InvoiceNumber },
                    { "Email", request.Metadata.Email },
                    { "EducationAccountId", request.Metadata.EducationAccountId }
                }
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            return new StripeCardPaymentResult
            {
                PaymentIntentId = paymentIntent.Id,
                ClientSecret = paymentIntent.ClientSecret,
                Currency = paymentIntent.Currency,
                Status = paymentIntent.Status
            };
        }

        public async Task<StripePayNowPaymentResult> CreatePayNowPaymentIntentAsync(StripePaymentRequest request)
        {
            var service = new PaymentIntentService();
            
            var createOptions = new PaymentIntentCreateOptions
            {
                Amount = (long)(request.Amount * 100),
                Currency = "sgd",
                PaymentMethodTypes = new List<string> { "paynow" },
                Metadata = new Dictionary<string, string>
                {
                    { "TransactionId", request.Metadata.TransactionId },
                    { "InvoiceNumber", request.Metadata.InvoiceNumber },
                    { "Email", request.Metadata.Email },
                    { "EducationAccountId", request.Metadata.EducationAccountId }
                },
                ConfirmationMethod = "automatic",
                Confirm = false
            };

            var paymentIntent = await service.CreateAsync(createOptions);

            var paymentMethodService = new PaymentMethodService();
            var paymentMethodOptions = new PaymentMethodCreateOptions
            {
                Type = "paynow"
            };
            var paymentMethod = await paymentMethodService.CreateAsync(paymentMethodOptions);

            var confirmOptions = new PaymentIntentConfirmOptions
            {
                PaymentMethod = paymentMethod.Id,
                ReturnUrl = _stripeSettings.PaymentSuccessUrl
            };
            
            paymentIntent = await service.ConfirmAsync(paymentIntent.Id, confirmOptions);

            var qrCodeUrl = "";
            var qrCodeData = "";
            var hostedInstructionsUrl = "";

            if (paymentIntent.NextAction?.Type == "paynow_display_qr_code")
            {
                var paynowAction = paymentIntent.NextAction.PaynowDisplayQrCode;
                qrCodeData = paynowAction?.Data ?? "";
                qrCodeUrl = paynowAction?.ImageUrlSvg ?? "";
                hostedInstructionsUrl = paynowAction?.HostedInstructionsUrl ?? "";
            }

            return new StripePayNowPaymentResult
            {
                PaymentIntentId = paymentIntent.Id,
                QRCodeUrl = qrCodeUrl,
                QRCodeData = qrCodeData,
                HostedInstructionsUrl = hostedInstructionsUrl,
                Status = paymentIntent.Status
            };
        }
    }
}
 