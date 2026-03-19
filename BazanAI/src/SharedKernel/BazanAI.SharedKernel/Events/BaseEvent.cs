
using System;
using BazanAI.SharedKernel.Domain;

namespace BazanAI.SharedKernel.Events;

public record BaseEvent<T>(
    Guid EventId,
    string EventType,
    string Version,
    string Source,
    string CorrelationId,
    DateTimeOffset OccurredAt,
    T Payload
) : IDomainEvent;
