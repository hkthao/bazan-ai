
using System;
using BazanAI.Infrastructure.Common.Messaging;
using BazanAI.SharedKernel.Events;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BazanAI.Infrastructure.Common.Security;
using BazanAI.SharedKernel.Security;

namespace BazanAI.Infrastructure.Common;

public static class DependencyInjection
{
    public static IServiceCollection AddBazanSecurity(this IServiceCollection services)
    {
        services.AddSingleton<IPiiEncryptionService, AesGcmPiiEncryptionService>();
        return services;
    }

    public static IServiceCollection AddBazanMassTransit(
        this IServiceCollection services, 
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configure = null)
    {
        var options = configuration.GetSection(MessagingOptions.SectionName).Get<MessagingOptions>() 
                      ?? new MessagingOptions();

        services.AddMassTransit(x =>
        {
            // Allow caller to add consumers, etc.
            configure?.Invoke(x);

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(options.Host, options.VirtualHost, h =>
                {
                    h.Username(options.Username);
                    h.Password(options.Password);
                });

                // Default retry policy: 3 times with exponential backoff
                cfg.UseMessageRetry(r => r.Exponential(
                    retryLimit: 3,
                    minInterval: TimeSpan.FromSeconds(1),
                    maxInterval: TimeSpan.FromSeconds(30),
                    intervalDelta: TimeSpan.FromSeconds(5)
                ));

                // Dead Letter Queue (Delayed Redelivery) after 3 retries fail
                // Note: MassTransit automatically handles error queues in RabbitMQ.
                // This is for longer-term retries.
                cfg.UseDelayedRedelivery(r => r.Intervals(
                    TimeSpan.FromMinutes(5),
                    TimeSpan.FromMinutes(30),
                    TimeSpan.FromHours(2)
                ));

                // Auto-configure endpoints for all consumers registered in 'x'
                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<IEventPublisher, RabbitMqPublisher>();

        return services;
    }
}
