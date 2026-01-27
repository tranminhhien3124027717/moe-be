using Microsoft.AspNetCore.Mvc;
using MOE_System.Application.Common;
using MOE_System.Application.DTOs.GlobalSettings.Request;
using MOE_System.Application.DTOs.GlobalSettings.Response;
using MOE_System.Application.Interfaces.Services;

namespace MOE_System.API.Controllers;

[ApiController]
[Route("api/v1/admin/settings-global")]
public class SettingGlobalController : BaseApiController
{
    private readonly IGlobalSettingsService _globalSettingsService;

    public SettingGlobalController(IGlobalSettingsService globalSettingsService)
    {
        _globalSettingsService = globalSettingsService;
    }

    /// <summary>
    /// Get global settings (billing date, due to date, day of closure)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<GlobalSettingsResponse>>> GetGlobalSettings(CancellationToken cancellationToken)
    {
        var settings = await _globalSettingsService.GetGlobalSettingsAsync(cancellationToken);
        return Success(settings, "Global settings retrieved successfully");
    }

    /// <summary>
    /// Update global settings (billing date, due to date, day of closure)
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<ApiResponse<GlobalSettingsResponse>>> UpdateGlobalSettings(
        [FromBody] UpdateGlobalSettingsRequest request, 
        CancellationToken cancellationToken)
    {
        var settings = await _globalSettingsService.UpdateGlobalSettingsAsync(request, cancellationToken);
        return Success(settings, "Global settings updated successfully");
    }

    /// <summary>
    /// Initialize default global settings (only if no settings exist)
    /// </summary>
    [HttpPost("initialize")]
    public async Task<ActionResult<ApiResponse>> InitializeDefaultSettings(CancellationToken cancellationToken)
    {
        await _globalSettingsService.InitializeDefaultSettingsAsync(cancellationToken);
        return Success("Default settings initialized successfully");
    }
}
