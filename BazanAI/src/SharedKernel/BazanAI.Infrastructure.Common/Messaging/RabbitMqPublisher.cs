
using System.Threading;
using System.Threading.Tasks;
using BazanAI.SharedKernel.Events;
using MassTransit;

namespace BazanAI.Infrastructure.Common.Messaging;

public class RabbitMqPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public RabbitMqPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishAsync<T>(T domainEvent, CancellationToken ct = default) where T : class
    {
        await _publishEndpoint.Publish(domainEvent, ct);
    }
}
