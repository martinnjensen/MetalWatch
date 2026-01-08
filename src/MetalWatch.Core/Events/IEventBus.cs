namespace MetalWatch.Core.Events;

/// <summary>
/// Interface for publishing and subscribing to domain events.
/// Provides decoupling between event producers and consumers.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes a domain event to all registered subscribers
    /// </summary>
    /// <typeparam name="TEvent">The type of domain event</typeparam>
    /// <param name="domainEvent">The event to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;

    /// <summary>
    /// Subscribes a handler to a specific type of domain event
    /// </summary>
    /// <typeparam name="TEvent">The type of domain event to subscribe to</typeparam>
    /// <param name="handler">The handler function to invoke when the event is published</param>
    void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
        where TEvent : IDomainEvent;
}
