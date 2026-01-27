using MOE_System.EService.Application.Common;
using MOE_System.EService.Domain.Enums;

namespace MOE_System.EService.Application.DTOs.EducationAccount
{
    #region Request

    public class BalanceHistoryRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public string SearchTerm { get; set; } = string.Empty;
        // Filter by type
        public ChangeType? Type { get; set; }

        // Date filter options
        public DateFilterType? DateFilter { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        // Amount filter options
        public AmountRangeType? AmountRange { get; set; }
        public decimal? AmountFrom { get; set; }
        public decimal? AmountTo { get; set; }
    }

    #endregion

    #region Response

    public class BalanceHistoryResponse
    {
        public PaginatedList<TransactionHistoryItem> History { get; set; } = null!;
    }

    public class TransactionHistoryItem
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string ReferenceId { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public string TransactionDate { get; set; } = string.Empty;
    }

    #endregion
}

