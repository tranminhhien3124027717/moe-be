namespace MOE_System.EService.Application.DTOs.EducationAccount
{
    public class TransactionDetailResponse
    {
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string ReferenceId { get; set; } = string.Empty;
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
    }
}
