using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MOE_System.EService.Application.Interfaces;
using MOE_System.EService.Application.Settings;
using MOE_System.EService.Infrastructure.Services;

namespace MOE_System.EService.API.ServicesRegister
{
    public static class StripeServiceRegistration
    {
        public static IServiceCollection AddStripeServiceRegistration(this IServiceCollection services, IConfiguration configuration)
        {
            var stripeSettings = configuration.GetSection("StripeSettings");

            if (stripeSettings == null)
            {
                throw new InvalidOperationException("Stripe settings are not configured properly.");
            }

            services.Configure<StripeSettings>(stripeSettings);
            services.AddScoped<IStripeService, StripeService>();

            return services;
        }
    }
}
