using MOE_System.Application.DTOs.GlobalSettings.Request;
using MOE_System.Application.DTOs.GlobalSettings.Response;

namespace MOE_System.Application.Interfaces.Services;

public interface IGlobalSettingsService
{
    Task<GlobalSettingsResponse> GetGlobalSettingsAsync(CancellationToken cancellationToken);
    Task<GlobalSettingsResponse> UpdateGlobalSettingsAsync(UpdateGlobalSettingsRequest request, CancellationToken cancellationToken);
    Task InitializeDefaultSettingsAsync(CancellationToken cancellationToken);
}
