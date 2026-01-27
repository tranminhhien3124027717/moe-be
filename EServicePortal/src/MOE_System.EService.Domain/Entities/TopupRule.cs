using MOE_System.EService.Domain.Enums;

namespace MOE_System.EService.Domain.Entities;

public class TopupRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string RuleName { get; set; } = string.Empty;
    public decimal TopupAmount { get; set; }
    public RuleTargetType RuleTargetType { get; set; } = RuleTargetType.Batch; // individual or batch
    public BatchFilterType? BatchFilterType { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal? MinBalance { get; set; }
    public decimal? MaxBalance { get; set; }

    public ICollection<EducationLevelDefinition> EducationLevels { get; set; } = new List<EducationLevelDefinition>();
    public ResidentialStatus? ResidentialStatus { get; set; }
    public ICollection<SchoolingStatusDefinition> SchoolingStatuses { get; set; } = new List<SchoolingStatusDefinition>();

    public DateTime ScheduledTime { get; set; }
    public bool IsExecuted { get; set; } = false;
    public int? NumberOfAccountsAffected { get; set; }
    public string? Description { get; set; }
    public string? InternalRemarks { get; set; }

    // Navigation properties
    public ICollection<BatchRuleExecution> BatchRuleExecutions { get; set; } = new List<BatchRuleExecution>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ICollection<TopupRuleTarget> Targets { get; set; } = new List<TopupRuleTarget>();
    public ICollection<TopupRuleAccountHolder> TopupRuleAccountHolders { get; set; } = new List<TopupRuleAccountHolder>();
    public ICollection<TopupExecutionSnapshot> TopupExecutionSnapshots { get; set; } = new List<TopupExecutionSnapshot>();

    // Use domain-level navigation manipulation instead of constructing join entities here.
    public void AddEducationLevel(EducationLevelDefinition educationLevel)
    {
        if (educationLevel == null) return;
        if (EducationLevels.Any(x => x.Id == educationLevel.Id)) return;
        EducationLevels.Add(educationLevel);
    }

    public void AddSchoolingStatus(SchoolingStatusDefinition schoolingStatus)
    {
        if (schoolingStatus == null) return;
        if (SchoolingStatuses.Any(x => x.Id == schoolingStatus.Id)) return;
        SchoolingStatuses.Add(schoolingStatus);
    }
}
