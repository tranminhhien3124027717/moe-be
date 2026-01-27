using MOE_System.EService.Domain.Common;

namespace MOE_System.EService.Domain.Entities;

public sealed class SchoolingStatusDefinition :BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ICollection<TopupRule> TopupRules { get; set; } = new List<TopupRule>();
}
