namespace MOE_System.EService.Application.Settings
{
    public class StripeSettings
    {
        public required string PublicKey { get; set; }
        public required string SecretKey { get; set; }
        public required string WebhookSecret { get; set; }
        public required string PaymentSuccessUrl { get; set; }
        public required string PaymentCancelUrl { get; set; }
    }
}
