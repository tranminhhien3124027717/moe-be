namespace MOE_System.Application.DTOs.Provider.Response;

public sealed record ProviderListResponse
(
    string ProviderId,
    string ProviderName,
    List<string> EducationLevels,
    List<SchoolingLevelDto> SchoolingLevels,
    string Status
);