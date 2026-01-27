using System.ComponentModel.DataAnnotations.Schema;

namespace MOE_System.EService.Domain.Entities;

[NotMapped]
public sealed class TopupRuleSchoolingStatus
{
    public string TopupRuleId { get; private set; } = null!;
    public string SchoolingStatusId { get; private set; } = null!;

    public TopupRule TopupRule { get; set; } = null!;
    public SchoolingStatusDefinition SchoolingStatusDefinition { get; set; } = null!;

    private TopupRuleSchoolingStatus() { }

    public TopupRuleSchoolingStatus(string topupRuleId, string schoolingStatusId)
    {
        TopupRuleId = topupRuleId;
        SchoolingStatusId = schoolingStatusId;
    }
}
