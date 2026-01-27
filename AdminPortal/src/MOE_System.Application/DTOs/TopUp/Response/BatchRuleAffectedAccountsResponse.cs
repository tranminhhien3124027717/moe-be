using MOE_System.Domain.Enums;

namespace MOE_System.Application.DTOs.TopUp.Response;

public sealed record BatchRuleAffectedAccountsResponse
{
    public int TotalAccounts { get; init; }
    public decimal AmountPerAccount { get; init; }
    public decimal TotalDisbursement { get; init; }
    public List<AffectedAccountDetailResponse> Accounts { get; init; } = new();
}

public sealed record AffectedAccountDetailResponse
{
    public string AccountHolderId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string NRIC { get; init; } = string.Empty;
    public decimal CurrentBalance { get; init; }
    public int Age { get; init; }
    public EducationLevel EducationLevel { get; init; }
    public SchoolingStatus SchoolingStatus { get; init; }
    public decimal TopupAmount { get; init; }
    public decimal NewBalance { get; init; }
}
