using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using MOE_System.Infrastructure.Data;
using MOE_System.Infrastructure.Repositories;
using MOE_System.Application.Common.Interfaces;
using MOE_System.Application.Interfaces;
using MOE_System.Infrastructure.Services;
using MOE_System.Application.Common;
using MOE_System.Infrastructure.Common;
using Quartz;
using MOE_System.Infrastructure.Jobs;
using MOE_System.Application.Common.Jobs;
using MOE_System.Infrastructure.BackgroundJobs;

namespace MOE_System.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Get connection string from configuration
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        
        // Register DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.Configure<AccountClosureOptions>(options =>
            configuration.GetSection("ClosureAccountOptions").Bind(options)
        );

        services.AddQuartz(q =>
        {
            q.UsePersistentStore(s =>
            {
                s.UseSqlServer(sqlServerOptions =>
                {
                    sqlServerOptions.ConnectionString = connectionString;
                    sqlServerOptions.TablePrefix = "QRTZ_";
                });
                s.UseNewtonsoftJsonSerializer();
            });

            var jobKey = new JobKey("AutoCloseEducationAccountJob");

            q.AddJob<AutoCloseEducationAccountJob>(opts => opts.WithIdentity(jobKey));

            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("AutoCloseEducationAccountTrigger")
                .WithSchedule(
                    CronScheduleBuilder.DailyAtHourAndMinute(0, 0)
                )
            );

            var invoiceJobKey = new JobKey("AutoCreateInvoiceJob");

            q.AddJob<AutoCreateInvoiceJob>(opts => opts.WithIdentity(invoiceJobKey));

            q.AddTrigger(opts => opts
                .ForJob(invoiceJobKey)
                .WithIdentity("AutoCreateInvoiceTrigger")
                .WithSchedule(
                    CronScheduleBuilder.DailyAtHourAndMinute(1, 0)
                )
            );
        });

        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register Generic Repository
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IClock, SystemClock>();
        services.AddScoped<ITopUpScheduler, TopUpScheduler>();

        services.AddHostedService<OutboxWorker>();
        services.AddHostedService<SchoolingStatusAutoTrigger>();
        services.AddHostedService<CourseStatusAutoTrigger>();
        services.AddHostedService<EducationLevelAutoTrigger>();

        return services;
    }
}
