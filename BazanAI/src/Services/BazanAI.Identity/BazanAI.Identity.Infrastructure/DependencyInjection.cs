using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BazanAI.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Infrastructure-specific registrations
        return services;
    }
}
