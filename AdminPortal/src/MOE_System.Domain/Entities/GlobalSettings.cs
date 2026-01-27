using MOE_System.Domain.Common;

namespace MOE_System.Domain.Entities;

public class GlobalSettings : BaseEntity
{
    public int BillingDate { get; set; } = 5;
    public int DueToDate { get; set; } = 15;
    
    // Account Creation Configuration (defaults to January 5th)
    public int CreationMonth { get; set; } = 1;
    public int CreationDay { get; set; } = 5;
    
    // Account Closure Configuration (defaults to December 31st)
    public int ClosureMonth { get; set; } = 12;
    public int ClosureDay { get; set; } = 31;
    
    [Obsolete("Use ClosureMonth and ClosureDay instead")]
    public string DayOfClosure { get; set; } = "12/31";
}
