using MOE_System.Domain.Enums;

namespace MOE_System.Domain.Entities;

public class BatchExecution
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime ScheduledTime { get; set; }
    public DateTime? ExecutedTime { get; set; }
    public TopUpStatus Status { get; set; } = TopUpStatus.Scheduled;

    // Navigation property
    public ICollection<BatchRuleExecution> BatchRuleExecutions { get; set; } = new List<BatchRuleExecution>();
}
