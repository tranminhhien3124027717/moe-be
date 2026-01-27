using MOE_System.Application.DTOs.Provider.Request;
using MOE_System.Application.DTOs.Provider.Response;

namespace MOE_System.Application.Interfaces.Services;

public interface IProviderService
{
    Task<IReadOnlyList<ProviderListResponse>> GetAllProvidersAsync(string? search, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProviderListResponse>> GetActiveProvidersAsync(string? search, CancellationToken cancellationToken);
    Task<ProviderDetailResponse> GetProviderByIdAsync(string id, CancellationToken cancellationToken);
    Task<IReadOnlyList<SchoolingLevelDto>> GetAllSchoolingLevelsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<SchoolingLevelDto>> GetSchoolingLevelsByProviderIdAsync(string providerId, CancellationToken cancellationToken);
    Task<ProviderDetailResponse> CreateProviderAsync(CreateProviderRequest request, CancellationToken cancellationToken);
    Task<ProviderDetailResponse> UpdateProviderAsync(UpdateProviderRequest request, CancellationToken cancellationToken);
    Task DeleteProviderAsync(string id, CancellationToken cancellationToken);
    Task<ProviderDetailResponse> ActivateProviderAsync(string id, CancellationToken cancellationToken);
    Task<ProviderDetailResponse> DeactivateProviderAsync(string id, CancellationToken cancellationToken);
}