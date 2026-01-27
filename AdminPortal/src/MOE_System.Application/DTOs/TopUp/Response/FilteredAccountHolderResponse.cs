namespace MOE_System.Application.DTOs.TopUp.Response;

public sealed record FilteredAccountHolderResponse
{
    public string Id { get; init; } = string.Empty;
    public string EducationAccountId { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string NRIC { get; init; } = string.Empty;
    public int Age { get; init; }
    public decimal Balance { get; init; }
    public string EducationLevel { get; init; } = string.Empty;
    public string SchoolingStatus { get; init; } = string.Empty;
}
