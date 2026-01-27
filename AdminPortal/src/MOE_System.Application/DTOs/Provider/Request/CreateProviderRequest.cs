namespace MOE_System.Application.DTOs.Provider.Request;

public sealed record CreateProviderRequest
{
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = "Active";
    public List<string> SchoolingLevelIds { get; init; } = new();
}
