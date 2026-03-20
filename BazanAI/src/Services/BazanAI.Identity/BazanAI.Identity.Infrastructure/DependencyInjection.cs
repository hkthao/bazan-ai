using BazanAI.Identity.Application.Auth.Common;
using BazanAI.Identity.Domain.Repositories;
using BazanAI.Identity.Infrastructure.ExternalServices;
using BazanAI.Identity.Infrastructure.Persistence;
using BazanAI.Identity.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BazanAI.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IFarmerRepository, FarmerRepository>();
        
        services.AddScoped<IOtpService, OtpService>();
        services.AddHttpClient<IZaloClient, ZaloClient>();

        return services;
    }
}
