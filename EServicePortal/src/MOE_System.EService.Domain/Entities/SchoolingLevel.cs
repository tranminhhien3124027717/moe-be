using MOE_System.EService.Domain.Common;

namespace MOE_System.EService.Domain.Entities;

public class SchoolingLevel : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Navigation property for M:M relationship
    public ICollection<Provider> Providers { get; set; } = new List<Provider>();
}
