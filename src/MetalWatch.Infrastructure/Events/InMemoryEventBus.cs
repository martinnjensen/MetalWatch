namespace MetalWatch.Infrastructure.Events;

using MetalWatch.Core.Events;
using Microsoft.Extensions.Logging;

/// <summary>
/// In-memory implementation of <see cref="IEventBus"/>.
/// Suitable for single-process applications and testing.
/// For distributed scenarios, consider using a message queue implementation.
/// </summary>
public class InMemoryEventBus : IEventBus
{
    private readonly ILogger<InMemoryEventBus> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="InMemoryEventBus"/>
    /// </summary>
    /// <param name="logger">Logger for diagnostics</param>
    public InMemoryEventBus(ILogger<InMemoryEventBus> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        // TODO: Implement event publishing
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
        where TEvent : IDomainEvent
    {
        // TODO: Implement subscription registration
    }
}
