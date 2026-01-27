using MOE_System.EService.Domain.Common;

namespace MOE_System.EService.Domain.Entities;

public class Provider : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    
    // Navigation properties
    public ICollection<Course> Courses { get; set; } = new List<Course>();
    public ICollection<SchoolingLevel> SchoolingLevels { get; set; } = new List<SchoolingLevel>();
}
