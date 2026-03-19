
using System;
using System.Collections.Generic;

namespace BazanAI.SharedKernel.Domain;

public abstract class Entity
{
    public string Id { get; protected set; } = Guid.NewGuid().ToString();
    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; protected set; }
}
