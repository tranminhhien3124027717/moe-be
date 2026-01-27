namespace MOE_System.EService.Domain.Entities;

public sealed class TopupRuleTarget
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TopupRuleId { get; set; } = string.Empty;
    public string EducationAccountId { get; set; } = string.Empty;

    public EducationAccount EducationAccount { get; set; } = null!;
    public TopupRule TopupRule { get; set; } = null!;
}
