using MOE_System.Application.DTOs.GlobalSettings.Request;
using MOE_System.Application.DTOs.GlobalSettings.Response;
using MOE_System.Application.Interfaces.Services;
using MOE_System.Domain.Entities;

namespace MOE_System.Application.Services;

public class GlobalSettingsService : IGlobalSettingsService
{
    private GlobalSettings _settings;
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

    public GlobalSettingsService()
    {
        // Initialize with default values when service is created (server start)
        _settings = new GlobalSettings
        {
            BillingDate = 5,
            DueToDate = 30,
            CreationMonth = 1,
            CreationDay = 5,
            ClosureMonth = 12,
            ClosureDay = 31,
            DayOfClosure = "12/31"
        };
    }

    public Task<GlobalSettingsResponse> GetGlobalSettingsAsync(CancellationToken cancellationToken)
    {
        var response = new GlobalSettingsResponse(
            _settings.Id,
            _settings.BillingDate,
            _settings.DueToDate,
            _settings.CreationMonth,
            _settings.CreationDay,
            _settings.ClosureMonth,
            _settings.ClosureDay,
            _settings.DayOfClosure,
            _settings.UpdatedAt
        );

        return Task.FromResult(response);
    }

    public async Task<GlobalSettingsResponse> UpdateGlobalSettingsAsync(UpdateGlobalSettingsRequest request, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Update settings in memory
            _settings.BillingDate = request.BillingDate;
            _settings.DueToDate = request.DueToDate;
            _settings.CreationMonth = request.CreationMonth;
            _settings.CreationDay = request.CreationDay;
            _settings.ClosureMonth = request.ClosureMonth;
            _settings.ClosureDay = request.ClosureDay;
            _settings.DayOfClosure = $"{request.ClosureMonth}/{request.ClosureDay}";
            _settings.UpdatedAt = DateTime.UtcNow;

            return new GlobalSettingsResponse(
                _settings.Id,
                _settings.BillingDate,
                _settings.DueToDate,
                _settings.CreationMonth,
                _settings.CreationDay,
                _settings.ClosureMonth,
                _settings.ClosureDay,
                _settings.DayOfClosure,
                _settings.UpdatedAt
            );
        }
        finally
        {
            _lock.Release();
        }
    }

    public Task InitializeDefaultSettingsAsync(CancellationToken cancellationToken)
    {
        // Settings are already initialized in constructor
        return Task.CompletedTask;
    }
}
