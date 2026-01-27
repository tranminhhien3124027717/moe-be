namespace MOE_System.Application.DTOs.TopUp.Response;

public sealed record TopupCustomizeFilterResponse
{
    public IReadOnlyList<EducationLevelDefinitionResponse> EducationLevels { get; init; } = new List<EducationLevelDefinitionResponse>();
    public IReadOnlyList<SchoolingStatusDefinitionResponse> SchoolingStatuses { get; init; } = new List<SchoolingStatusDefinitionResponse>();
}

public sealed class EducationLevelDefinitionResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public sealed class SchoolingStatusDefinitionResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}