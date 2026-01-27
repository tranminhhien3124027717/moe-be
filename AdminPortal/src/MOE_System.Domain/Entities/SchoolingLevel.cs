using MOE_System.Domain.Common;

namespace MOE_System.Domain.Entities;

public class SchoolingLevel : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Navigation property for M:M relationship
    public ICollection<Provider> Providers { get; set; } = new List<Provider>();
}
