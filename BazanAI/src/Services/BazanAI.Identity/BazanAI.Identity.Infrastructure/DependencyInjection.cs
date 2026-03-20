using BazanAI.Identity.Domain.Repositories;
using BazanAI.Identity.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BazanAI.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IFarmerRepository, FarmerRepository>();
        // Infrastructure-specific registrations
        return services;
    }
}
