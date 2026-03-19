
using System.Threading;
using System.Threading.Tasks;

namespace BazanAI.SharedKernel.Events;

public interface IEventPublisher
{
    Task PublishAsync<T>(T domainEvent, CancellationToken ct = default) where T : class;
}
