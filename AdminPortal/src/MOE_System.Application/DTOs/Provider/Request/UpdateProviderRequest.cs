namespace MOE_System.Application.DTOs.Provider.Request;

public sealed record UpdateProviderRequest
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public List<string> SchoolingLevelIds { get; init; } = new();
}
