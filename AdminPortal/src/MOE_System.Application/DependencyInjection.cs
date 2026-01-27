using Microsoft.Extensions.DependencyInjection;
using MOE_System.Application.Interfaces;
using MOE_System.Application.Services;
using MOE_System.Application.Interfaces.Services;
using FluentValidation;

namespace MOE_System.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(typeof(MOE_System.Application.Common.Events.CheckUserStatusEvent).Assembly);
        });

        // Register Application services
        services.AddScoped<IAccountHolderService, AccountHolderService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IEducationAccountService, EducationAccountService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<IProviderService, ProviderService>();
        services.AddScoped<ITopUpService, TopUpService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddSingleton<IGlobalSettingsService, GlobalSettingsService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IStudentStatusService, StudentStatusService>();
        services.AddScoped<ICourseStatusService, CourseStatusService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITopUpConfigService, TopUpConfigService>();

        return services;
    }
}
