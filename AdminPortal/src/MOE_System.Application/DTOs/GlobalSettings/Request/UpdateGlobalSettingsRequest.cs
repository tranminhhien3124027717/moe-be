namespace MOE_System.Application.DTOs.GlobalSettings.Request;

public sealed record UpdateGlobalSettingsRequest
{
    public int BillingDate { get; init; }
    public int DueToDate { get; init; }
    
    // Account Creation Configuration
    public int CreationMonth { get; init; } = 1;
    public int CreationDay { get; init; } = 5;
    
    // Account Closure Configuration
    public int ClosureMonth { get; init; } = 12;
    public int ClosureDay { get; init; } = 31;
    
    [Obsolete("Use ClosureMonth and ClosureDay instead")]
    public string DayOfClosure { get; init; } = string.Empty;
}
