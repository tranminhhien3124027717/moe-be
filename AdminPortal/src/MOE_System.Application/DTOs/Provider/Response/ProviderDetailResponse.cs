namespace MOE_System.Application.DTOs.Provider.Response;

public sealed record ProviderDetailResponse
(
    string Id,
    string Name,
    string Status,
    List<SchoolingLevelDto> SchoolingLevels,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public sealed record SchoolingLevelDto
(
    string Id,
    string Name,
    string Description
);
