using System.ComponentModel.DataAnnotations.Schema;

namespace MOE_System.Domain.Entities;

[NotMapped]
public sealed class TopupRuleEducationLevel
{
    public string TopupRuleId { get; set; } = null!;
    public string EducationLevelId { get; set; } = null!;

    public TopupRule TopupRule { get; set; } = null!;
    public EducationLevelDefinition EducationLevelDefinition { get; set; } = null!;

    private TopupRuleEducationLevel() { }

    public TopupRuleEducationLevel(string topupRuleId, string educationLevelId)
    {
        TopupRuleId = topupRuleId;
        EducationLevelId = educationLevelId;
    }
}