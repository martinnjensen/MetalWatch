namespace MetalWatch.Infrastructure.Events;

using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<Type, List<object>> _handlers = new();

    /// <summary>
    /// Initializes a new instance of <see cref="InMemoryEventBus"/>
    /// </summary>
    /// <param name="logger">Logger for diagnostics</param>
    public InMemoryEventBus(ILogger<InMemoryEventBus> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        var eventType = typeof(TEvent);

        if (!_handlers.TryGetValue(eventType, out var handlers))
        {
            _logger.LogDebug("No handlers registered for event type {EventType}", eventType.Name);
            return;
        }

        _logger.LogDebug("Publishing {EventType} to {HandlerCount} handler(s)", eventType.Name, handlers.Count);

        foreach (var handler in handlers)
        {
            var typedHandler = (Func<TEvent, CancellationToken, Task>)handler;
            await typedHandler(domainEvent, cancellationToken);
        }
    }

    /// <inheritdoc />
    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
        where TEvent : IDomainEvent
    {
        var eventType = typeof(TEvent);

        _handlers.AddOrUpdate(
            eventType,
            _ => new List<object> { handler },
            (_, existing) =>
            {
                existing.Add(handler);
                return existing;
            });

        _logger.LogDebug("Subscribed handler for event type {EventType}", eventType.Name);
    }
}
