using Microsoft.AspNetCore.Mvc;
using MOE_System.Application.Common;
using MOE_System.Application.DTOs.Provider.Request;
using MOE_System.Application.DTOs.Provider.Response;
using MOE_System.Application.Interfaces.Services;

namespace MOE_System.API.Controllers;

[ApiController]
[Route("api/v1/admin/providers")]
public class ProviderController : BaseApiController
{
    private readonly IProviderService _providerService;

    public ProviderController(IProviderService providerService)
    {
        _providerService = providerService;
    }

    /// <summary>
    /// Get all providers (list view) with optional search
    /// </summary>
    /// <param name="search">Search term to filter providers by name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProviderListResponse>>>> GetAllProviders([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var providers = await _providerService.GetAllProvidersAsync(search, cancellationToken);
        return Success(providers, "Providers retrieved successfully");
    }

    /// <summary>
    /// Get active providers only with optional search
    /// </summary>
    /// <param name="search">Search term to filter providers by name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("active")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProviderListResponse>>>> GetActiveProviders([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var providers = await _providerService.GetActiveProvidersAsync(search, cancellationToken);
        return Success(providers, "Active providers retrieved successfully");
    }

    /// <summary>
    /// Get all schooling levels
    /// </summary>
    [HttpGet("schooling-levels")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SchoolingLevelDto>>>> GetAllSchoolingLevels(CancellationToken cancellationToken)
    {
        var schoolingLevels = await _providerService.GetAllSchoolingLevelsAsync(cancellationToken);
        return Success(schoolingLevels, "Schooling levels retrieved successfully");
    }

    /// <summary>
    /// Get provider by ID with full details including schooling levels
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ProviderDetailResponse>>> GetProviderById([FromRoute] string id, CancellationToken cancellationToken)
    {
        var provider = await _providerService.GetProviderByIdAsync(id, cancellationToken);
        return Success(provider, "Provider retrieved successfully");
    }

    /// <summary>
    /// Get schooling levels by provider ID
    /// </summary>
    [HttpGet("{id}/schooling-levels")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SchoolingLevelDto>>>> GetSchoolingLevelsByProviderId([FromRoute] string id, CancellationToken cancellationToken)
    {
        var schoolingLevels = await _providerService.GetSchoolingLevelsByProviderIdAsync(id, cancellationToken);
        return Success(schoolingLevels, "Schooling levels retrieved successfully");
    }

    /// <summary>
    /// Create a new provider with multiple schooling levels
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProviderDetailResponse>>> CreateProvider([FromBody] CreateProviderRequest request, CancellationToken cancellationToken)
    {
        var provider = await _providerService.CreateProviderAsync(request, cancellationToken);
        return Created(provider, "Provider created successfully");
    }

    /// <summary>
    /// Update an existing provider
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ProviderDetailResponse>>> UpdateProvider([FromRoute] string id, [FromBody] UpdateProviderRequest request, CancellationToken cancellationToken)
    {
        // Ensure ID from route matches request body
        if (id != request.Id)
        {
            return BadRequest(ApiResponse.ErrorResponse("Provider ID in route does not match request body"));
        }

        var provider = await _providerService.UpdateProviderAsync(request, cancellationToken);
        return Success(provider, "Provider updated successfully");
    }

    /// <summary>
    /// Delete a provider
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> DeleteProvider([FromRoute] string id, CancellationToken cancellationToken)
    {
        await _providerService.DeleteProviderAsync(id, cancellationToken);
        return Success("Provider deleted successfully");
    }

    /// <summary>
    /// Activate a provider
    /// </summary>
    [HttpPatch("{id}/activate")]
    public async Task<ActionResult<ApiResponse<ProviderDetailResponse>>> ActivateProvider([FromRoute] string id, CancellationToken cancellationToken)
    {
        var provider = await _providerService.ActivateProviderAsync(id, cancellationToken);
        return Success(provider, "Provider activated successfully");
    }

    /// <summary>
    /// Deactivate a provider
    /// </summary>
    [HttpPatch("{id}/deactivate")]
    public async Task<ActionResult<ApiResponse<ProviderDetailResponse>>> DeactivateProvider([FromRoute] string id, CancellationToken cancellationToken)
    {
        var provider = await _providerService.DeactivateProviderAsync(id, cancellationToken);
        return Success(provider, "Provider deactivated successfully");
    }
}
