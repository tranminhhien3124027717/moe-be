namespace MOE_System.Application.DTOs.GlobalSettings.Response;

public sealed record GlobalSettingsResponse
(
    string Id,
    int BillingDate,
    int DueToDate,
    int CreationMonth,
    int CreationDay,
    int ClosureMonth,
    int ClosureDay,
    [property: Obsolete("Use ClosureMonth and ClosureDay instead")]
    string DayOfClosure,
    DateTime? UpdatedAt
);
